using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// A single step in a step-up banner.
    /// Each step has its own cost multiplier, pull count, and optional guaranteed rarity.
    /// Matches ToS step-up gacha: progressive steps with escalating rewards.
    /// </summary>
    public class StepUpStep
    {
        public int StepIndex { get; }
        public float CostMultiplier { get; }   // 0 = free, 0.5 = half price, 1.0 = normal
        public int PullCount { get; }           // How many pulls this step gives
        public int GuaranteedMinRarity { get; } // 0 = no guarantee, >0 = at least one pull at this rarity

        public StepUpStep(int stepIndex, float costMultiplier, int pullCount, int guaranteedMinRarity = 0)
        {
            StepIndex = stepIndex;
            CostMultiplier = costMultiplier;
            PullCount = pullCount;
            GuaranteedMinRarity = guaranteedMinRarity;
        }

        public StepUpStep(Dictionary<string, object> data, int index)
        {
            StepIndex = index;
            CostMultiplier = data.TryGetValue("cost_mult", out var cm) ? Convert.ToSingle(cm) : 1f;
            PullCount = data.TryGetValue("pull_count", out var pc) ? Convert.ToInt32(pc) : 1;
            GuaranteedMinRarity = data.TryGetValue("guaranteed_rarity", out var gr) ? Convert.ToInt32(gr) : 0;
        }
    }

    /// <summary>
    /// Step-up banner that wraps a GachaPool with progressive steps.
    /// Players must complete steps in order. Each step may have different costs,
    /// pull counts, and guarantees. After completing all steps, the banner is done.
    ///
    /// Typical ToS step-up pattern:
    ///   Step 1: Free x1 pull
    ///   Step 2: Half price x3 pulls
    ///   Step 3: Normal price x5 pulls
    ///   Step 4: Normal price x5 pulls, guaranteed 5★
    ///   Step 5: Normal price x10 pulls, guaranteed featured
    /// </summary>
    public class StepUpBanner
    {
        public int Id { get; }
        public string Name { get; }
        public GachaPool BasePool { get; }
        public List<StepUpStep> Steps { get; }
        public int CurrentStep { get; private set; }
        public bool IsComplete => CurrentStep >= Steps.Count;

        public StepUpBanner(int id, string name, GachaPool basePool, List<StepUpStep> steps)
        {
            Id = id;
            Name = name;
            BasePool = basePool;
            Steps = steps ?? new List<StepUpStep>();
            CurrentStep = 0;
        }

        public StepUpBanner(Dictionary<string, object> data, GachaPool basePool)
        {
            Id = data.TryGetValue("id", out var idVal) ? Convert.ToInt32(idVal) : 0;
            Name = data.TryGetValue("name", out var nameVal) ? Convert.ToString(nameVal) : "";
            BasePool = basePool;
            CurrentStep = data.TryGetValue("current_step", out var cs) ? Convert.ToInt32(cs) : 0;

            Steps = new List<StepUpStep>();
            if (data.TryGetValue("steps", out var stepsVal) && stepsVal is List<object> stepList)
            {
                for (int i = 0; i < stepList.Count; i++)
                {
                    if (stepList[i] is Dictionary<string, object> stepDict)
                        Steps.Add(new StepUpStep(stepDict, i));
                }
            }
        }

        /// <summary>
        /// Get the current step's configuration. Returns null if banner is complete.
        /// </summary>
        public StepUpStep GetCurrentStep()
        {
            if (IsComplete) return null;
            return Steps[CurrentStep];
        }

        /// <summary>
        /// Get the cost for the current step (base pool cost * step multiplier).
        /// </summary>
        public int GetCurrentCost()
        {
            var step = GetCurrentStep();
            if (step == null) return 0;
            return (int)(BasePool.CostAmount * step.CostMultiplier) * step.PullCount;
        }

        /// <summary>
        /// Execute the current step. Rolls the appropriate number of pulls
        /// with optional guaranteed rarity. Advances to next step.
        /// </summary>
        public List<GachaResult> ExecuteStep(int pityCount, Random rng)
        {
            var step = GetCurrentStep();
            if (step == null) return new List<GachaResult>();

            var results = GachaRoller.RollMulti(BasePool, step.PullCount, pityCount, rng);

            // Apply guaranteed minimum rarity if specified
            if (step.GuaranteedMinRarity > 0)
            {
                bool hasGuaranteed = false;
                foreach (var r in results)
                {
                    if (r.Rarity >= step.GuaranteedMinRarity)
                    {
                        hasGuaranteed = true;
                        break;
                    }
                }

                if (!hasGuaranteed && results.Count > 0)
                {
                    // Replace the last result with a guaranteed pull
                    var guaranteed = GachaRoller.RollMinRarity(BasePool, step.GuaranteedMinRarity, rng);
                    results[results.Count - 1] = guaranteed;
                }
            }

            CurrentStep++;
            return results;
        }

        /// <summary>
        /// Reset the banner to step 0 (for banners that can be repeated).
        /// </summary>
        public void Reset()
        {
            CurrentStep = 0;
        }

        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object>
            {
                ["id"] = Id,
                ["current_step"] = CurrentStep,
            };
        }
    }
}
