using System;

[Serializable]
public class SaveScoreRequest
{
    public int userId;
    public int score;
}

[Serializable]
public class SaveScoreResponse
{
    public string status;
    public string message;
    public int scoreId;
}

[Serializable]
public class HighScoreResponse
{
    public string status;
    public int userId;
    public int highScore;
}

