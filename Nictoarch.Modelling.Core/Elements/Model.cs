using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;

namespace Nictoarch.Modelling.Core.Elements
{
    public sealed class Model
    {
        public string name { get; }

        public IReadOnlyList<Entity> entities { get; }
        public IReadOnlyList<Link> links { get; }

        public Model(string name, IReadOnlyList<Entity> entities, IReadOnlyList<Link> links)
        {
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.entities = entities ?? throw new ArgumentNullException(nameof(entities));
            this.links = links ?? throw new ArgumentNullException(nameof(links));

            try
            {
                this.Validate();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in model {this.name}: {ex.Message}", ex);
            }
        }

        private void Validate()
        {
            //entity duplicates
            {
                HashSet<string> entityIds = new HashSet<string>(this.entities.Count);
                foreach (Entity entity in this.entities)
                {
                    if (!entityIds.Add(entity.semantic_id))
                    {
                        throw new Exception($"Found entites with same {nameof(Entity.semantic_id)} ('{entity.semantic_id}')");
                    }
                }
            }

            //link duplicates
            {
                HashSet<string> linkIds = new HashSet<string>(this.links.Count);
                foreach (Link link in this.links)
                {
                    if (!linkIds.Add(link.semantic_id))
                    {
                        throw new Exception($"Found links with same {nameof(Entity.semantic_id)} ('{link.semantic_id}')");
                    }
                }
            }
        }

        public JObject ToJson()
        {
            JObject result = new JObject();
            result.Add(nameof(this.name), new JValue(this.name));
            
            JArray entitiesArray = new JArray(this.entities.Count);
            foreach (Entity entity in this.entities)
            {
                entitiesArray.Add(entity.ToJson());
            }

            result.Add(nameof(this.entities), entitiesArray);

            JArray linksArray = new JArray(this.links.Count);
            foreach (Link link in this.links)
            {
                linksArray.Add(link.ToJson());
            }

            result.Add(nameof(this.links), linksArray);

            return result;
        }

        public override string ToString()
        {
            return this.ToJson().ToIndentedString();
        }

        public static async Task<Model> FromJson(Stream stream)
        {
            return (await JsonSerializer.DeserializeAsync<Model>(stream))!;
        }
    }
}
