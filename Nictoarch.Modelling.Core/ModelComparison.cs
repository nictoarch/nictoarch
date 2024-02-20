using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Nictoarch.Modelling.Core.Elements;
using System.Text.Json;
using Jsonata.Net.Native.Json;
using System.CommandLine;
using YamlDotNet.Core.Tokens;
using System.Text.Json.JsonDiffPatch;

namespace Nictoarch.Modelling.Core
{
    public sealed class ModelComparison
    {
        public sealed class PropDiff
        {
            public Entity @ref { get; set; } = default!;
            public Entity check { get; set; } = default!;
            public IDictionary<string, object> diffs { get; set; } = default!;
        }

        public string ref_model_name { get; }
        public string check_model_name { get; }
        public int entities_in_ref_count { get; }
        public int entities_in_check_count { get; }
        public int entities_equal_count { get; }
        public int entities_not_in_check_count => this.entities_not_in_check.Count;
        public int entities_not_in_ref_count => this.entities_not_in_ref.Count;
        public int entities_different_properties_count => this.entities_different_properties.Count;
        public IReadOnlyList<Entity> entities_not_in_check { get; }
        public IReadOnlyList<Entity> entities_not_in_ref { get; }
        public IReadOnlyList<PropDiff> entities_different_properties { get; }

        public ModelComparison(Model refModel, Model checkModel)
        {
            this.entities_not_in_check = refModel.entities
                .Where(re => !checkModel.entities.Any(ce => ce.GetIdentityKey() == re.GetIdentityKey()))
                .ToList();
            this.entities_not_in_ref = checkModel.entities
                .Where(ce => !refModel.entities.Any(re => re.GetIdentityKey() == ce.GetIdentityKey()))
                .ToList();

            this.entities_different_properties = refModel.entities
                .Select(re => new PropDiff() { @ref = re, check = checkModel.entities.FirstOrDefault(ce => ce.GetIdentityKey() == re.GetIdentityKey())! })
                .Where(d => d.check != null)
                .Select(d => {
                    d.diffs = this.CalculateDiff(d.@ref.properties, d.check.properties)!;
                    return d;
                })
                .Where(d => d.diffs != null && d.diffs.Count > 0)
                .ToList();

            this.ref_model_name = refModel.name;
            this.check_model_name = checkModel.name;

            this.entities_in_ref_count = refModel.entities.Count;
            this.entities_in_check_count = checkModel.entities.Count;
            this.entities_equal_count = this.entities_in_ref_count - this.entities_not_in_check_count - this.entities_different_properties_count;
        }

        private IDictionary<string, object>? CalculateDiff(Dictionary<string, object>? refProps, Dictionary<string, object>? checkProps)
        {
            if (refProps == null && checkProps == null)
            {
                return null;
            }
            if (refProps == null)
            {
                return new Dictionary<string, object>() { [$"{nameof(PropDiff.@ref)}"] = "no properties" };
            }
            if (checkProps == null)
            {
                return new Dictionary<string, object>() { [$"{nameof(PropDiff.check)}"] = "has no properties" };
            }

            JsonNode refNode = JsonSerializer.SerializeToNode(refProps)!;
            JsonNode checkNode = JsonSerializer.SerializeToNode(checkProps)!;
            JsonNode? diff = refNode.Diff(checkNode);

            if (diff == null)
            {
                return null;
            }

            IDictionary<string, object> res = diff.AsObject()!
                .ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value!);
            return res;
        }

        public bool ModelsAreSame()
        {
            return this.entities_not_in_ref_count == 0
                && this.entities_not_in_check_count == 0
                && this.entities_different_properties_count == 0;
        }
    }
}
