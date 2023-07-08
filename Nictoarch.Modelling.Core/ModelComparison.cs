using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nictoarch.Modelling.Core.Elements;

namespace Nictoarch.Modelling.Core
{
    public sealed class ModelComparison
    {
        public string ref_model_name { get; }
        public string check_model_name { get; }
        public IReadOnlyList<Entity> entities_not_in_check { get; }
        public IReadOnlyList<Entity> entities_not_in_ref { get; }

        public ModelComparison(
            string refModelName, 
            string checkModelName, 
            IReadOnlyList<Entity> entitiesNotInCheck, 
            IReadOnlyList<Entity> entitiesNotInRef
        )
        {
            this.ref_model_name = refModelName;
            this.check_model_name = checkModelName;
            this.entities_not_in_check = entitiesNotInCheck;
            this.entities_not_in_ref = entitiesNotInRef;
        }

        public bool ModelsAreSame()
        {
            return this.entities_not_in_ref.Count == 0
                && this.entities_not_in_check.Count == 0;
        }

        public static ModelComparison Build(Model refModel, Model checkModel)
        {
            List<Entity> entitiesNotInCheck = refModel.entities
                .Where(re => !checkModel.entities.Any(ce => ce.semantic_id == re.semantic_id))
                .ToList();
            List<Entity> entitiesNotInRef = checkModel.entities
                .Where(ce => !refModel.entities.Any(re => ce.semantic_id == re.semantic_id))
                .ToList();


            return new ModelComparison(
                refModelName: refModel.name,
                checkModelName: checkModel.name,
                entitiesNotInCheck: entitiesNotInCheck,
                entitiesNotInRef: entitiesNotInRef
            );
        }
    }
}
