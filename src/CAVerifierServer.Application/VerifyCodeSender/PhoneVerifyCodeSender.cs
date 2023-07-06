using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CAVerifierServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUglify.JavaScript.Syntax;
using Volo.Abp.Sms;

namespace CAVerifierServer.VerifyCodeSender;

public class PhoneVerifyCodeSender : IVerifyCodeSender
{
    public string Type => "Phone";
    private readonly ILogger<PhoneVerifyCodeSender> _logger;
    private readonly IEnumerable<ISMSServiceSender> _smsServiceSender;
    private readonly SmsServiceOptions _smsServiceOptions;
    private const string ChineseRegionNum = "+86";
    private readonly MobileCountryRegularCategoryOptions _mobileCountryRegularCategoryOptions;

    public PhoneVerifyCodeSender(ILogger<PhoneVerifyCodeSender> logger,
        IEnumerable<ISMSServiceSender> smsServiceSender,
        IOptions<SmsServiceOptions> smsServiceOptions,
        IOptions<MobileCountryRegularCategoryOptions> mobileCountryRegularCategoryOptions)
    {
        _logger = logger;
        _smsServiceSender = smsServiceSender;
        _mobileCountryRegularCategoryOptions = mobileCountryRegularCategoryOptions.Value;
        _smsServiceOptions = smsServiceOptions.Value;
    }

    public async Task SendCodeByGuardianIdentifierAsync(string guardianIdentifier, string code)
    {
        var countryName = "";
        foreach (var category in _mobileCountryRegularCategoryOptions.MobileInfos)
        {
            var regex = new Regex(category.MobileRegular);
            if (regex.IsMatch(guardianIdentifier))
            {
                countryName = category.Country;
            }

            break;
        }


        var smsServiceDic = _smsServiceOptions.SmsServiceInfos
            .Where(o => o.Value.SupportingCountries.Contains(countryName))
            .OrderByDescending(k => k.Value.Ratio)
            .ToDictionary(o => o.Key, o => o.Value.Ratio);

        if (smsServiceDic.Count == 0)
        {
            _logger.LogError("No sms service provider is enable");
            return;
        }

        var failedServicesCount = 0;
        foreach (var smsServiceSender in smsServiceDic.Keys.Select(smsServiceSenderName =>
                     _smsServiceSender.FirstOrDefault(o => o.ServiceName == smsServiceSenderName)))
        {
            if (smsServiceSender == null)
            {
                _logger.LogError("Can not find sms service provider {serviceName}", smsServiceDic.FirstOrDefault().Key);
                return;
            }

            try
            {
                _logger.LogDebug("Choose sms service provider is : {serviceName}", smsServiceSender.ServiceName);
                await smsServiceSender.SendAsync(new SmsMessage(guardianIdentifier, code));
                break;
            }
            catch (Exception e)
            {
                _logger.LogDebug("{serviceName} sending sms failed : Error:{e}", smsServiceSender.ServiceName,
                    e.Message);
                failedServicesCount += 1;
                if (failedServicesCount < smsServiceDic.Count)
                {
                    continue;
                }

                _logger.LogError("All sms service provider sending sms failed");
                throw e;
            }
        }
    }


    public bool ValidateGuardianIdentifier(string guardianIdentifier)
    {
        return !string.IsNullOrWhiteSpace(guardianIdentifier);
    }
}