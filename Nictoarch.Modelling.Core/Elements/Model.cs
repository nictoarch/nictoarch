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
        public string name { get; set; }

        public List<Entity> entities { get; set; }
        public List<Link> links { get; set; }
        public List<object> invalid_objects { get; set; }

        public Model()
        {
            this.name = default!;
            this.entities = default!;
            this.links = default!;
            this.invalid_objects = default!;
        }

        public Model(string name, List<Entity> entities, List<Link> links, List<object> invalid_objects)
        {
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.entities = entities ?? throw new ArgumentNullException(nameof(entities));
            this.links = links ?? throw new ArgumentNullException(nameof(links));
            this.invalid_objects = invalid_objects ?? throw new ArgumentNullException(nameof(invalid_objects));

            try
            {
                this.Validate(checkLinkIntegrity: false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in model {this.name}: {ex.Message}", ex);
            }
        }

        private void Validate(bool checkLinkIntegrity)
        {
            //entity duplicates
            HashSet<IEntityKey> entityKeys = new HashSet<IEntityKey>(this.entities.Count, IEntityKey.Comparer);
            foreach (Entity entity in this.entities)
            {
                if (!entityKeys.Add(entity))
                {
                    throw new Exception($"Found entites with same identity key ('{entity.GetKeyString()}')");
                }
            }

            //link duplicates
            {
                HashSet<ILinkKey> linkIds = new HashSet<ILinkKey>(this.links.Count, ILinkKey.Comparer);
                foreach (Link link in this.links)
                {
                    if (!linkIds.Add(link))
                    {
                        throw new Exception($"Found links with same identity key ('{link.GetKeyString()}')");
                    }

                    if (checkLinkIntegrity)
                    {
                        if (!entityKeys.Contains(link.from))
                        {
                            throw new Exception($"No '{nameof(link.from)}' entity found for link '{link.GetKeyString()}' (from: {link.from.GetKeyString()})");
                        }
                        if (!entityKeys.Contains(link.to))
                        {
                            throw new Exception($"No '{nameof(link.to)}' entity found for link '{link.GetKeyString()}' (to: {link.to.GetKeyString()})");
                        }
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


            JArray invalidsArray = new JArray(this.invalid_objects.Count);
            foreach (object invalid in this.invalid_objects)
            {
                if (invalid is JToken token)
                {
                    invalidsArray.Add(token);
                }
                else
                {
                    invalidsArray.Add(JToken.FromObject(invalid));
                }
            }
            result.Add(nameof(this.invalid_objects), invalidsArray);


            return result;
        }

        public override string ToString()
        {
            return this.ToJson().ToIndentedString(Jsonata.Constants.NO_NULLS);
        }

        public static Model FromJson(Stream stream)
        {
            JObject modelObj;
            using (TextReader reader = new StreamReader(stream))
            {
                modelObj = (JObject)JToken.Parse(reader);
            }
            Model result = modelObj.ToObject<Model>(Jsonata.Constants.ALLOW_MISSING);
            return result;
        }
    }
}
