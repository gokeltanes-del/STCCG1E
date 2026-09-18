using System;
using StarTrekCCG;

class Program {
  static int Main() {
    var err = DilemmaRules.VerifyTemporalCausalityLoop();
    Console.WriteLine(err == null ? "VerifyTemporalCausalityLoop: OK" : "VerifyTemporalCausalityLoop FAIL: " + err);
    return err == null ? 0 : 1;
  }
}
