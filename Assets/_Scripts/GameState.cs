public enum GameMode
{
	SinglePlayer,
	Multiplayer
}

public static class GameState
{
	public static GameMode CurrentMode { get; set; } = GameMode.Multiplayer;
}