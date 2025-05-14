using System.IO;
using UnityEngine;

public static class ResponseLogger
{
    private static string filePath = Path.Combine(Application.persistentDataPath, "responses.csv");

    // The constructor checks if the file exists, and if it doesn't, creates the CSV file with headers.
    static ResponseLogger()
    {
        if (!File.Exists(filePath))
        {
            // Create the CSV file with the columns for responses
            File.WriteAllText(filePath, "AnimationType,Variation,Response1,Response2,Response3\n");
        }
    }

    // Method to log responses to the CSV file
    public static void LogResponse(string animType, int variation, string[] responses)
    {
        // Create a line to add to the CSV file
        string line = $"{animType},{variation},{responses[0]},{responses[1]},{responses[2]}";
        
        // Append the line to the CSV file
        File.AppendAllText(filePath, line + "\n");

        // Log a message in the console for debugging
        Debug.Log($"[Logger] Responses logged: {line}");
    }
}
