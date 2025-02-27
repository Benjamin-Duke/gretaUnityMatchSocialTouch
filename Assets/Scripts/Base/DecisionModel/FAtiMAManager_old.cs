using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ActionLibrary;
using AutobiographicMemory;
using Conditions;
using GAIPS.Rage;
using IntegratedAuthoringTool;
using RolePlayCharacter;
using UnityEngine;
using UnityEngine.Serialization;
using WellFormedNames;

public class FAtiMAManager_old : MonoBehaviour
{
    [Tooltip("JSON file name in StreamingAssets folder")]
    public string rulesFile;

    [Tooltip("JSON file name in StreamingAssets folder")]
    public string scenarioFile;

    [Tooltip("Name of the character in the model controlled by a human")]
    public string humanCharacter;

    [Tooltip("Name of the agent in the model controlled by the decision model")]
    public string agentCharacter;
    
    [Tooltip(
        "If true, FAtiMA will take action for the human characters, given the model rules (probably choose randomly between the options. This is useful for testing purposes. If false, you need to trigger the actions manually, probably following a Unity event (collision, movement, etc). This is what happens in real experiments.")]
    public bool simulateHumanActions;
    
    [Tooltip("Print all actions sent to FAtiMA for debugging purposes")]
    public bool debug = true;

    // Number of completed actions, to keep track in Unity
    public int ActionsDone { get; private set; }

    // Tells if at the moment, at least one character
    // has an action to do
    public bool ActionLeft { get; private set; }

    // This variable is the only reference to FAtiMA state and storage.
    // Each time we want to evaluate something, take a decision, get a knowledge, we will use this variable.
    public IntegratedAuthoringToolAsset Model { get; private set; }

    // Load model
    public void Start()
    {
        var cogRulesPath = $"{Application.streamingAssetsPath}/{rulesFile}";
        var scenarioPath = $"{Application.streamingAssetsPath}/{scenarioFile}";

        // Load rules
        var rules = AssetStorage.FromJson(File.ReadAllText(cogRulesPath));

        // Load scenario and link to rules storage
        Model = IntegratedAuthoringToolAsset.FromJson(File.ReadAllText(scenarioPath), rules);
    }

    // Start the simulation
    public void StartScenario()
    {
        if (!Model.Characters.Any())
        {
            Debug.LogWarning("Won't start simulation : no characters in model");
            return;
        }

        foreach (var character in Model.Characters)
        {
            // Each character has its own "database" (asset) of 
            // emotional appraisal, decision making, etc. Load it
            // from main rules asset.
            character.LoadAssociatedAssets(Model.Assets);
            // Each asset is associated with dynamic properties.
            // Merge them into the main asset.
            Model.BindToRegistry(character.DynamicPropertiesRegistry);
        }

        // Build an initial event ("Enters") for each
        // agent in the simulation, and send the events
        // to all characters
        var enterEvents = Model.Characters.Select(c => EventHelper.ActionEnd(c.CharacterName.ToString(), "Enters", "-"))
            .ToList();

        foreach (var character in Model.Characters)
        {
            character.Perceive(enterEvents);
            if(debug) Debug.Log($"{character.CharacterName} enters.");
        }

        ActionsDone = 1;
        ActionLeft = true;
    }

    // Advance the simulation by a "tick".
    // Will decide what to do for each agent and apply a **single** decision, if any.
    // You need to call this function until ActionLeft == false,
    // if you want all possible actions (given the current world state) to be applied.
    //
    // Returns an Enumerator containing all actions performed by agents, so
    // that these actions can be translated into Unity actions.
    public IEnumerable<Name> Advance()
    {
        if (ActionsDone == 0)
        {
            Debug.LogWarning("Cannot advance simulation since it is not started yet.");
            yield break;
        }

        if(debug) Debug.Log($"Number of actions completed : {ActionsDone}");
        foreach (var character in Model.Characters)
            // Should we "decide" for the character, if he's an human ?
            if (simulateHumanActions || humanCharacter != character.CharacterName.ToString())
            {
                // If so, get possible actions given current world state
                var actions = character.Decide().ToList();
                if (actions.Any())
                {
                    // Choose the first action, which has the highest priority
                    DoAction(character, actions.First());
                    yield return BuildEvent(character, actions.First());
                }

                ActionLeft = actions.Count > 1;
            }
    }

    // Trigger a single action for a specific character.
    // This function should be mainly used by Unity scripts which 
    // triggers human's actions in reaction to an event (collision, etc).
    private void DoAction(RolePlayCharacterAsset character, IAction action, string actionType = AMConsts.ACTION_END)
    {
        // Perceive action and trigger potential effects
        HandleActionEffects(BuildEvent(character, action, actionType));
        // Then, handle potential "event triggers" (e.g. no more dialogue)
        HandleEventTriggers();
        // Increment the character's internal tick and decay mood
        character.Update();
    }

    // Wrapper around the private method which will look for
    // a registered character with the given name and execute the action
    public void DoAction(string subjectName, IAction action, string actionType = AMConsts.ACTION_END)
    {
        var character = Model.Characters.FirstOrDefault(c => c.CharacterName == (Name) subjectName);
        if (character == null)
            throw new ArgumentException(
                $"Character {subjectName} not in model ({Model.Characters.Select(c => c.CharacterName)})");
        DoAction(character, action, actionType);
    }

    public void StopScenario()
    {
        if (ActionsDone != 0)
        {
            ActionsDone = 0;
            if(debug) Debug.Log("Simulation stopped.");
        }
    }

    // Triggers an action event, get all resulting effects (i.e. property changes),
    // and trigger property-changed events for observing agent(s)
    // Observing agents seems to be agents that are concerned about properties of
    // other agents ; this function won't inform the agent whose properties have been changed.
    private void HandleActionEffects(Name eventToTrigger)
    {
        // Trigger the action event
        foreach (var character in Model.Characters) character.Perceive(eventToTrigger);

        if(debug) Debug.Log($"Action perceived by all agents : {eventToTrigger}");
        // Get all properties modified by the action
        var possibleEffects = Model.WorldModel.Simulate(eventToTrigger);
        foreach (var possibleEffect in possibleEffects)
        {
            var applyEffects = true;
            // Iterate over conditions
            foreach (var c in possibleEffect.Item1)
            foreach (var a in Model.Characters)
                // Allowed to ask the evaluation of conditions for all agents
                // in simulation
                if (c.Agent == a.CharacterName.ToString() || c.Agent == Name.UNIVERSAL_STRING)
                {
                    var cond = Condition.Parse(c.Condition);
                    // If any condition is false, none of the effects will be applied
                    if (!cond.Evaluate(a.m_kb, Name.SELF_SYMBOL, new List<SubstitutionSet>{c.Substitutions})) applyEffects = false;
                }

            if (!applyEffects) continue;

            foreach (var effect in possibleEffect.Item2)
                // Check if the property change must be perceived by another agent
            foreach (var character in Model.Characters)
                if (effect.ObserverAgent == character.CharacterName ||
                    effect.ObserverAgent == Name.UNIVERSAL_SYMBOL)
                {
                    // If we get there, the change of property
                    // must be perceived by the agent
                    var newValue = effect.NewValue.ToString();
                    // Evaluate new value if needed (eg Dynamic property)
                    if (effect.NewValue.IsComposed) newValue = character.GetBeliefValue(newValue);
                    // Name of the agent concerned by the property change
                    var actor = eventToTrigger.GetNTerm(2).ToString();
                    // Build property change event
                    var propertyChangedEvent = EventHelper.PropertyChange(
                        effect.PropertyName.ToString(),
                        newValue,
                        ((decimal)effect.NewCertainty).ToString(CultureInfo.InvariantCulture),
                        actor
                    );
                    // Trigger the property changed event
                    character.Perceive(propertyChangedEvent);

                    if(debug) Debug.Log(
                        $"Property {effect.PropertyName} of {actor} is now {newValue} with certainty {effect.NewCertainty} (observed by {effect.ObserverAgent})");
                }
        }
    }

    // Event triggers are special events triggered when
    // a player runs out of dialogue options, for example.
    // We need to handle them at each tick.
    private void HandleEventTriggers()
    {
        if (Model.eventTriggers == null) Model.eventTriggers = new EventTriggers();

        var events = Model.eventTriggers.ComputeTriggersList(Model.Characters.ToList());

        foreach (var eventTrigger in events) HandleActionEffects(eventTrigger);
    }

    private Name BuildEvent(RolePlayCharacterAsset character, IAction action, string actionType = AMConsts.ACTION_END)
    {
        // If the action is a dialogue step, we need to build
        // a custom Action-End event with all dialogue step information
        if (action.Key.ToString() == IATConsts.DIALOG_ACTION_KEY)
        {
            var dialogue = Model.GetDialogAction(action, out var error);
            if (error != null)
                Debug.LogError($"Error while getting dialog {action.Name} : {error}");
            else
                if(debug) Debug.Log(
                    $"{character.CharacterName} to {action.Target} : {character.ProcessWithBeliefs(dialogue.Utterance)}");
            var eventString =
                $"Speak({dialogue.CurrentState},{dialogue.NextState},{dialogue.Meaning},{dialogue.Style})";
            return EventHelper.ActionEnd(character.CharacterName.ToString(), eventString,
                action.Target.ToString());
        }

        if(debug) Debug.Log($"{character.CharacterName} will perform {actionType} : {action}");
        switch (actionType)
        {
            case AMConsts.ACTION_END:
                return EventHelper.ActionEnd(character.CharacterName, action.Name, action.Target);
            case AMConsts.ACTION_UPDATE:
                return EventHelper.ActionUpdate(character.CharacterName, action.Name, action.Target);
            case AMConsts.ACTION_START:
                return EventHelper.ActionStart(character.CharacterName, action.Name, action.Target);
            default:
                throw new ArgumentException($"{actionType} is not a valid action type.");
        }
    }
}
