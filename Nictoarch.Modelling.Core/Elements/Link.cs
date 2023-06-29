using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.Modelling.Core.Elements
{
    public sealed class Link
    {
        public readonly Entity from;
        public readonly Entity to;
        public readonly string domainId;
        public readonly string semanticId;
        public readonly string displayText;

        public Link(Entity from, Entity to, string domainId, string semanticId, string displayText)
        {
            this.from = from ?? throw new ArgumentNullException(nameof(from));
            this.to = to ?? throw new ArgumentNullException(nameof(to));
            this.domainId = domainId ?? throw new ArgumentNullException(nameof(domainId));
            this.semanticId = semanticId ?? throw new ArgumentNullException(nameof(semanticId));
            this.displayText = displayText ?? throw new ArgumentNullException(nameof(displayText));
        }
    }
}
