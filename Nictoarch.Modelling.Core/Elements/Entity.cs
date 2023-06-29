using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.Core.Elements
{
    public sealed class Entity
    {
        public readonly string type;
        public readonly string domainId;
        public readonly string semanticId;
        public readonly string displayName;

        public Entity(string type, string domainId, string semanticId, string displayName)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.domainId = domainId ?? throw new ArgumentNullException(nameof(domainId));
            this.semanticId = semanticId ?? throw new ArgumentNullException(nameof(semanticId));
            this.displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        }
    }
}
