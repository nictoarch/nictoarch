using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Nictoarch.Modelling.Core.Elements;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json;

namespace Nictoarch.Modelling.Core
{
    public sealed class ModelComparison
    {
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
        public IReadOnlyList<Tuple<Entity, Entity?>> entities_different_properties { get; }

        public ModelComparison(Model refModel, Model checkModel)
        {
            this.entities_not_in_check = refModel.entities
                .Where(re => !checkModel.entities.Any(ce => ce.GetIdentityKey() == re.GetIdentityKey()))
                .ToList();
            this.entities_not_in_ref = checkModel.entities
                .Where(ce => !refModel.entities.Any(re => re.GetIdentityKey() == ce.GetIdentityKey()))
                .ToList();

            this.entities_different_properties = refModel.entities
                .Select(re => Tuple.Create(re, checkModel.entities.FirstOrDefault(ce => ce.GetIdentityKey() == re.GetIdentityKey())))
                .Where(t => t.Item2 != null)
                .Where(t => !PropertiesEqual(t.Item1, t.Item2!))
                .ToList();

            this.ref_model_name = refModel.name;
            this.check_model_name = checkModel.name;

            this.entities_in_ref_count = refModel.entities.Count;
            this.entities_in_check_count = checkModel.entities.Count;
            this.entities_equal_count = this.entities_in_ref_count - this.entities_not_in_check_count - this.entities_different_properties_count;
        }

        public bool ModelsAreSame()
        {
            return this.entities_not_in_ref_count == 0
                && this.entities_not_in_check_count == 0
                && this.entities_different_properties_count == 0;
        }

        private sealed class EtitiyPropertiesElementComparer : IEqualityComparer<KeyValuePair<string, object>>
        {
            internal static readonly EtitiyPropertiesElementComparer Instance = new EtitiyPropertiesElementComparer();

            bool IEqualityComparer<KeyValuePair<string, object>>.Equals(KeyValuePair<string, object> x, KeyValuePair<string, object> y)
            {
                if (x.Key != y.Key)
                {
                    return false;
                }
                object xValue = x.Value;
                object yValue = y.Value;

                if (xValue == null && yValue == null)
                {
                    return true;
                }
                if (xValue == null || yValue == null)
                {
                    return false;
                }

                Type valueType = xValue.GetType();
                if (valueType != yValue.GetType())
                {
                    return false;
                }

                if (xValue is JsonElement xJsonElement)
                {
                    JsonElement yJsonElement = (JsonElement)yValue;
                    bool result = xJsonElement.DeepEquals(yJsonElement);
                    return result;
                }
                else if (xValue is JsonNode xJsonNode)
                {
                    JsonValue yJsonNode = (JsonValue)yValue;
                    bool result = xJsonNode.DeepEquals(yJsonNode);
                    return result;
                }
                else
                {
                    IEqualityComparer comparer = (IEqualityComparer)(typeof(EqualityComparer<>)
                        .MakeGenericType(valueType)
                        .GetProperty(nameof(EqualityComparer<object>.Default))!
                        .GetValue(null)!
                    );
                    bool result = comparer.Equals(xValue, yValue);
                    return result;
                }
            }

            int IEqualityComparer<KeyValuePair<string, object>>.GetHashCode(KeyValuePair<string, object> obj)
            {
                throw new NotImplementedException();
            }
        }

        private static bool PropertiesEqual(Entity refEntity, Entity checkEntity)
        {
            if (refEntity.properties == null && checkEntity.properties == null)
            {
                return true;
            }
            if (refEntity.properties == null || checkEntity.properties == null)
            {
                return false;
            }

            bool result = refEntity.properties.OrderBy(i => i.Key)
                    .SequenceEqual(checkEntity.properties.OrderBy(i => i.Key), EtitiyPropertiesElementComparer.Instance);

            return result;
        }
    }
}
