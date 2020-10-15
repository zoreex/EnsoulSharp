using EnsoulSharp;

namespace mAxIO
{
    using EnsoulSharp.SDK;

    public class Program
    {
        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
            Game.Print("mAxIO by ZrX loaded.");
        }

        private static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName == "Ashe")
            {
                maxioAshe.OnGameLoad();
            }
        }
    }
}
