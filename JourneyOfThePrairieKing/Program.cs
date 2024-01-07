namespace JourneyOfThePrairieKing
{
   class Program
   {
      static void Main(string[] args)
      {
         using (Game game = new Game(1920, 1080, "Preria king"))
         {
            game.UpdateFrequency = 500;
            game.Run();
         }
      }
   }
}

