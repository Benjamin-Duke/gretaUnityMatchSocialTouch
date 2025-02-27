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
using Action = ActionLibrary.Action;

public class ProcessedFAtiMAAction
{
    public string identifier;
    public string content;
    public string target;
    public string nextState;
}
public class FAtiMAManager : MonoBehaviour
{
    [Tooltip("JSON file name in StreamingAssets folder")]
    public string rulesFile = "DecisionModel/Experiment_Basic_CGRL.json";

    [Tooltip("JSON file name in StreamingAssets folder")]
    public string scenarioFile = "DecisionModel/Experiment_Basic_Sc.json";

    [Tooltip("Name of the character in the model controlled by a human")]
    public string humanCharacter;

    [Tooltip("Name of the agent in the model controlled by the decision model")]
    public string agentCharacter;

    [Tooltip("Print all actions sent to FAtiMA for debugging purposes")]
    public bool debug = true;

    // Number of completed actions by all characters, to keep track in Unity
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

        ActionsDone = 0;
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
    }

    // To update the emotional decay of a character, we increase the tick count in FAtiMA.
    // The tick count is a purely abstract, ad-hoc value uncorrelated with the actual passing of time.
    // It should therefore either be used: - On each decision/action
    // - On a timed schedule left to the appreciation of the developer and managed by Unity
    public void TickAdvance(RolePlayCharacterAsset character)
    {
        if (ActionsDone != 0)
        {
            character.Update();
        }
    }

    // Helper to trigger a human action to the agent (automatically fill the subject and target fields)
    public Name DoHumanAction(string actionName, IEnumerable<Name> actionArgs,
        string actionType = AMConsts.ACTION_END)
    {
         return DoAction(humanCharacter, agentCharacter, actionName, actionArgs, actionType);
         //return DoAvailableAction(agentCharacter);
    }

    // Wrapper around the other DoAction method for easier construct of Action object
    // Trigger a single action for a specific character and apply conditionnal effects.
    // subjectName and targetName must refer to valid Characters 
    // actionName and actionArgs must be understood as : actionName(actionArgs[0], actionArgs[1], ....)
    public Name DoAction(string subjectName, string targetName, string actionName, IEnumerable<Name> actionArgs, string actionType = AMConsts.ACTION_END)
    {
        if (ActionsDone == 0)
        {
            if (debug) 
                Debug.LogWarning("Cannot do action simulation since it is not started yet.");
            return Name.NIL_SYMBOL;
        }
        //if(debug) Debug.Log($"Number of actions completed : {ActionsDone}");
        
        // Build action to send from subject to target
        var actionParams = new List<Name> {(Name) actionName};
        actionParams.AddRange(actionArgs);
        var action = new Action(actionParams, (Name) targetName);
        var subject = GetCharacter(subjectName);
        var evenmt = BuildEvent(subject, action, actionType);

        // Do the initial action
        DoAction(evenmt, subject);
        return evenmt;
    }
    
    // Trigger a single action for a specific character and apply conditionnal effects.
    public void DoAction(Name evenmt, RolePlayCharacterAsset character)
    {
        if (ActionsDone == 0)
        {
            if (debug)
                Debug.LogWarning("Cannot do action simulation since it is not started yet.");
            return;
        }
        // Perceive action and trigger potential effects
        HandleActionEffects(evenmt);
        // Then, handle potential "event triggers" (e.g. no more dialogue)
        //HandleEventTriggers();
        // Increment the character's internal tick and decay mood
        character.Update();
        ActionsDone++;
    }

    // To simplify calling for the agent's decision and action
    public Name DoAgentAction()
    {
        return DoAvailableAction(agentCharacter);
    }

    // To simplify calling for the agent's decision and non-speak action
    public Name DoAgentNonSpeakAction()
    {
        return DoAvailableNonSpeakAction(agentCharacter);
    }


    // Check if the agent has an action to do : if so, do it and return the corresponding event name
    public Name DoAvailableAction(string agentName)
    {
        var agent = GetCharacter(agentName);
        return DoAvailableAction(agent);
    }

    // Check if the agent has a non-speak action to do : if so, do it and return the corresponding event name
    public Name DoAvailableNonSpeakAction(string agentName)
    {
        var agent = GetCharacter(agentName);
        return DoAvailableNonSpeakAction(agent);
    }

    // Check if the agent has an action to do : if so, do it and return the corresponding event name
    public Name DoAvailableAction(RolePlayCharacterAsset agent)
    {
        var actions = agent.Decide().ToList();
        if (actions.Any())
        {
            // Choose the first action, which has the highest priority
            var evenmt = BuildEvent(agent, actions.First());
            DoAction(evenmt, agent);
            return evenmt;
        }
        return Name.NIL_SYMBOL;
    }

    // Check if the agent has an action to do : if so, do it and return the corresponding event name
    public Name DoAvailableNonSpeakAction(RolePlayCharacterAsset agent)
    {
        var actions = agent.Decide().ToList();
        if (actions.Any())
        {
            // Choose the first action, which has the highest priority
            var evenmt = BuildEvent(agent, actions.First());
            if (IsDialogEvent(evenmt))
            {
                return Name.NIL_SYMBOL;
            }
            else
            {
                DoAction(evenmt, agent);
                return evenmt;
            }
        }
        return Name.NIL_SYMBOL;
    }
    
    // Wrapper around the other ActionsAvailable for easier use
    public bool ActionsAvailable(string agentName)
    {
        var agent = GetCharacter(agentName);
        return ActionsAvailable(agent);
    }
    
    // Tells if at the moment, the agent given in parameter has an action available
    public bool ActionsAvailable(RolePlayCharacterAsset agent)
    {
        return agent.Decide().ToList().Count >= 1;
    }
    
    public void StopScenario()
    {
        if (ActionsDone != 0)
        {
            ActionsDone = 0;
            var actionArgs = new List<Name>();
            actionArgs.Add((Name)"Shape1");
            actionArgs.Add((Name)"Shape2");
            DoHumanAction(
                "FullReset",
                actionArgs,
                AMConsts.ACTION_END
            );
            if(debug) Debug.Log("Simulation reset and stopped.");
        }
    }

    public bool IsScenarioStarted()
    {
        return (ActionsDone != 0);
    }
    
    // Is the given event name a dialog action ? Returns true if yes, and false if no.
    public static bool IsDialogEvent(Name action)
    {
        if (action.GetTerms().Count() < 4)
            throw new ArgumentException($"Name {action} does not seem to be an action!");
        return action.GetNTerm(3).GetFirstTerm().ToString() == IATConsts.DIALOG_ACTION_KEY;
    }

    // Translate the FAtiMA formatted action into a format that Unity can more easily use to generate the actions in a perceivable way.
    // Ex: If action is dialog, extract the utterance and launch another action to make FAtiMA decide on the modality, then aggregate the two to form the name of the corresponding FML file.
    public ProcessedFAtiMAAction ProcessAction(Name evt)
    {
        //We want to extract the most important (for Unity handling) parameters of the action done : the action type (identifier)
        //the content to perform (utterance, FML, etc.), the target and the next state of the interaction.
        var processedAction = new ProcessedFAtiMAAction();
        if (evt == Name.NIL_SYMBOL)
        {
            processedAction.identifier = "Error";
            processedAction.content = "No action from FAtiMA";
            return processedAction;
        }
        var actionIdentifier = evt.GetNTerm(3).GetFirstTerm().ToString();
        List<Name> actionTerms = evt.GetNTerm(3).GetTerms().ToList();
        var actionTarget = evt.GetNTerm(4);
        processedAction.identifier = actionIdentifier;
        processedAction.target = actionTarget.ToString();

        if (IsDialogEvent(evt))
        {
            var action = new Action(actionTerms, actionTarget);
            var dialogue = Model.GetDialogAction(action, out var error);
            if (actionTarget == (Name)agentCharacter)
            {
                processedAction.content = dialogue.Utterance;
            }
            else
            {
                string FML = dialogue.Utterance;
                FML += ProcessAction(DoAgentAction()).content;
                processedAction.content = FML;
            }

            processedAction.nextState = dialogue.NextState;
            return processedAction;
        }
        else if (actionIdentifier == "Modality")
        {
            processedAction.content = actionTerms.ElementAt(1).ToString();
            return processedAction;
        }
        else if (actionIdentifier == "Backchannel")
        {
            switch (actionTerms.ElementAt(1).ToString())
            {
                case "Touch":
                    processedAction.content = "SimpleTouchR";
                    break;
                case "Nod":
                    processedAction.content = "Nod";
                    break;
                case "Smile":
                    processedAction.content = "Joy";
                    break;
                case "Sad":
                    processedAction.content = "Sad";
                    break;
                default:
                    processedAction.content = "Nod";
                    break;
            }
            return processedAction;
        }
        else
        {
            return processedAction;
        }
    }
    
    //Returns the RPCAsset corresponding to the character name given as parameter if it exists.
    private RolePlayCharacterAsset GetCharacter(string characterName)
    {
        var character = Model.Characters.FirstOrDefault(c => c.CharacterName == (Name) characterName);
        if (character == null)
            throw new ArgumentException(
                $"Character {characterName} not in model ({Model.Characters.Select(c => c.CharacterName)})");
        return character;
    }
    
    // Triggers an action event, get all resulting effects (i.e. property changes),
    // and trigger property-changed events for observing agent(s)
    // Observing agents are specified in the world model rules corresponding to the event
    // this function won't inform the agent whose properties have been changed unless it is specified as an observing agent.
    private void HandleActionEffects(Name eventToTrigger)
    {
        // Trigger the action event
        foreach (var character in Model.Characters) character.Perceive(eventToTrigger);

        if(debug) Debug.Log($"Action perceived by all agents : {eventToTrigger}");
        // Get all properties modified by the action
        var possibleEffects = Model.WorldModel.Simulate(eventToTrigger);
        var observerAgents = new Dictionary<string, List<Name>>(); // To store the agents involved and the effects they need to perceive
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
                    if (!cond.Evaluate(a.m_kb, Name.SELF_SYMBOL, null)) applyEffects = false;
                }

            if (!applyEffects) continue;

            foreach (var effect in possibleEffect.Item2)
                // Check if the property change must be perceived by another agent
                //Because multiple rules can be activated at the same time, we cannot perceive effects until after all the possible rules' conditions have been evaluated.
                //We store the validated effects for each observer agent here.
            foreach (var character in Model.Characters)
                if (effect.ObserverAgent == character.CharacterName ||
                    effect.ObserverAgent == Name.UNIVERSAL_SYMBOL)
                {
                    // If we get there, the change of property
                    // must be perceived by the agent
                    // Evaluate new value if needed (eg Dynamic property)
                    var newValue = effect.NewValue.IsComposed ? character.GetBeliefValue(effect.NewValue.ToString()) : effect.NewValue.ToString();
                    // Build property change event
                    var propertyChangedEvent = EventHelper.PropertyChange(
                        effect.PropertyName.ToString(),
                        newValue,
                        ((decimal)effect.NewCertainty).ToString(CultureInfo.InvariantCulture),
                        character.CharacterName.ToString()
                    );
                    if (!observerAgents.ContainsKey(character.CharacterName.ToString()))
                        observerAgents.Add(character.CharacterName.ToString(),
                            new List<Name> {propertyChangedEvent});
                    else observerAgents[character.CharacterName.ToString()].Add(propertyChangedEvent);
                }
        }
        var toWrite = "";
        toWrite += "Effects: \n";
        foreach (var o in observerAgents)
        {
            toWrite += o.Key + ": ";
            //We iterate over all the effects of the event and make the agents perceive them all
            foreach (var e in o.Value)
            {
                var agent = Model.Characters.FirstOrDefault(x => x.CharacterName.ToString() == o.Key);
                if (agent == null) continue;
                agent.Perceive(e);
                var value = agent.GetBeliefValue(e.GetNTerm(3).ToString());
                toWrite += e.GetNTerm(3).ToString() + " = " + value + ", ";
            }

            toWrite = toWrite.Substring(0, toWrite.Length - 2);
            toWrite += "\n";
        }
        if(debug) Debug.Log(toWrite);
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

    //Builds a well formed name for the action passed as parameter.
    //The well formed name thus built can then be sent to FAtiMA to progress the interaction.
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
                $"{IATConsts.DIALOG_ACTION_KEY}({dialogue.CurrentState},{dialogue.NextState},{dialogue.Meaning},{dialogue.Style})";
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