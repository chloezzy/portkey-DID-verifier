using System.Collections.Generic;

namespace CAVerifierServer.Options;

public class SmsServiceOptions
{
    public Dictionary<string, SmsServiceOption> SmsServiceInfos { get; set; }
}

public class SmsServiceOption
{
    public List<string> SupportingCountries { get; set; }

    public int Ratio { get; set; }
}