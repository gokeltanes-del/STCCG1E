using System;
using StarTrekCCG;
class P {
  static int Main() {
    var a = DilemmaRules.VerifyAlienParasites1a();
    var b = DilemmaRules.VerifyHyperAgingQuarantine();
    Console.WriteLine(a == null ? "Parasites VERIFY OK" : ("Parasites FAIL: " + a));
    Console.WriteLine(b == null ? "HyperAging VERIFY OK" : ("HyperAging FAIL: " + b));
    return (a == null && b == null) ? 0 : 1;
  }
}
