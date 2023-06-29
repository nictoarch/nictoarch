using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.Core.Elements
{
    public sealed class Model
    {
        public readonly string displayName;

        public readonly IReadOnlyList<Entity> entities;
        public readonly IReadOnlyList<Link> links;

        public Model(string displayName, IReadOnlyList<Entity> entities, IReadOnlyList<Link> links)
        {
            this.displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            this.entities = entities ?? throw new ArgumentNullException(nameof(entities));
            this.links = links ?? throw new ArgumentNullException(nameof(links));
        }
    }
}
