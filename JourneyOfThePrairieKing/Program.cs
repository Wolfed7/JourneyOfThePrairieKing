using System;

namespace JourneyOfThePrairieKing
{
   class Program
   {
      static void Main(string[] args)
      {
         using (Game game = new Game())
         {
            game.UpdateFrequency = 500;
            game.Run();
         }
      }
   }
}

