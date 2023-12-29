using System;

namespace JourneyOfThePrairieKing
{
   class Program
   {
      static void Main(string[] args)
      {
         using (Game game = new Game())
         {
            game.UpdateFrequency = 1000;
            game.Run();
         }
      }
   }

}

