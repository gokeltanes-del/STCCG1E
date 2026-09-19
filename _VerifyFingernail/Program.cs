using System;
using StarTrekCCG;

class Program
{
    static int Main()
    {
        int fails = 0;
        void Check(string name, string? err)
        {
            if (err == null)
            {
                Console.WriteLine("OK  " + name);
            }
            else
            {
                Console.WriteLine("FAIL " + name + ": " + err);
                fails++;
            }
        }

        Check("Lore's Fingernail", EventRules.VerifyLoresFingernail());
        Check("Microvirus (IsInorganic)", DilemmaRules.VerifyMicrovirus());
        Check("Holo-Projectors (no regress)", EventRules.VerifyHoloProjectors());

        return fails == 0 ? 0 : 1;
    }
}
