﻿using Bit.Api.Models.Response;
using Bit.Core;
using Bit.Core.Context;
using Bit.Core.Enums;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Api.Controllers;

[Route("plans")]
[Authorize("Web")]
public class PlansController : Controller
{
    private readonly ITaxRateRepository _taxRateRepository;
    private readonly IFeatureService _featureService;
    private readonly ICurrentContext _currentContext;

    public PlansController(
        ITaxRateRepository taxRateRepository,
        IFeatureService featureService,
        ICurrentContext currentContext)
    {
        _taxRateRepository = taxRateRepository;
        _featureService = featureService;
        _currentContext = currentContext;
    }

    [HttpGet("")]
    [AllowAnonymous]
    public ListResponseModel<PlanResponseModel> Get()
    {
        var plansUpgradeIsEnabled = _featureService.IsEnabled(FeatureFlagKeys.BillingPlansUpgrade, _currentContext);
        var teamsStarterPlanIsEnabled = _featureService.IsEnabled(FeatureFlagKeys.BillingStarterPlan, _currentContext);
        var responses = StaticStore.Plans
            // If plans upgrade is disabled, return only the original plans. Otherwise, return everything
            .Where(plan => plansUpgradeIsEnabled || plan.Type <= PlanType.EnterpriseAnnually2020 || plan.Type == PlanType.TeamsStarter)
            // If teams starter is disabled, don't return that plan, otherwise return everything
            .Where(plan => teamsStarterPlanIsEnabled || plan.Product != ProductType.TeamsStarter)
            .Select(plan =>
            {
                if (!plansUpgradeIsEnabled && plan.Type is <= PlanType.EnterpriseAnnually2020 and >= PlanType.TeamsMonthly2020)
                {
                    plan.LegacyYear = null;
                }
                else if (plansUpgradeIsEnabled && plan.Type is <= PlanType.EnterpriseAnnually2020 and >= PlanType.TeamsMonthly2020)
                {
                    plan.LegacyYear = 2023;
                }
                return new PlanResponseModel(plan);
            });
        return new ListResponseModel<PlanResponseModel>(responses);
    }

    [HttpGet("sales-tax-rates")]
    public async Task<ListResponseModel<TaxRateResponseModel>> GetTaxRates()
    {
        var data = await _taxRateRepository.GetAllActiveAsync();
        var responses = data.Select(x => new TaxRateResponseModel(x));
        return new ListResponseModel<TaxRateResponseModel>(responses);
    }
}
