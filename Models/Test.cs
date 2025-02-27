using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.FhirPath;
using Newtonsoft.Json;
using FhirDeathRecord;

namespace canary.Models
{
    public class Test
    {
        public int TestId { get; set; }

        public DateTime Created { get; set; }

        public DateTime CompletedDateTime { get; set; }

        public bool CompletedBool { get; set; }

        public int Total { get; set; }

        public int Correct { get; set; }

        public int Incorrect { get; set; }

        public Record ReferenceRecord { get; set; }

        public Record TestRecord { get; set; }

        public string Results { get; set; }

        public string Type { get; set; }

        public Test()
        {
            Created = DateTime.Now;
            Total = 0;
            Correct = 0;
            Incorrect = 0;
            CompletedBool = false;
            ReferenceRecord = new Record();
            ReferenceRecord.Populate();
        }

        public Test Run(string description)
        {
            TestRecord = new Record(DeathRecord.FromDescription(description));
            Compare();
            CompletedDateTime = DateTime.Now;
            CompletedBool = true;
            return this;
        }

        public void Compare()
        {
            Dictionary<string, Dictionary<string, dynamic>> description = new Dictionary<string, Dictionary<string, dynamic>>();
            foreach(PropertyInfo property in typeof(DeathRecord).GetProperties().OrderBy(p => ((Property)p.GetCustomAttributes().First()).Priority))
            {
                // Grab property annotation for this property
                Property info = (Property)property.GetCustomAttributes().First();

                // Skip properties that shouldn't be serialized.
                if (!info.Serialize)
                {
                    continue;
                }

                // Skip properties that are lost in the IJE format (if the test is a roundtrip).
                if (!info.CapturedInIJE && (Type != null && Type.Contains("Roundtrip")))
                {
                    continue;
                }

                // Add category if it doesn't yet exist
                if (!description.ContainsKey(info.Category))
                {
                    description.Add(info.Category, new Dictionary<string, dynamic>());
                }

                // Add the new property to the category
                Dictionary<string, dynamic> category = description[info.Category];
                category[property.Name] = new Dictionary<string, dynamic>();

                // Add the attributes of the property
                category[property.Name]["Name"] = info.Name;
                category[property.Name]["Type"] = info.Type.ToString();
                category[property.Name]["Description"] = info.Description;

                // Add snippets for reference
                FHIRPath path = (FHIRPath)property.GetCustomAttributes().Last();
                var matches = ReferenceRecord.GetRecord().GetITypedElement().Select(path.Path);
                if (matches.Count() > 0)
                {
                    if (info.Type == Property.Types.TupleCOD)
                    {
                        // Make sure to grab all of the Conditions for COD
                        string xml = "";
                        string json = "";
                        foreach(var match in matches)
                        {
                            xml += match.ToXml();
                            json += match.ToJson() + ",";
                        }
                        category[property.Name]["SnippetXML"] = xml;
                        category[property.Name]["SnippetJSON"] = "[" + json + "]";
                    }
                    else if (!String.IsNullOrWhiteSpace(path.Element))
                    {
                        // Since there is an "Element" for this path, we need to be more
                        // specific about what is included in the snippets.
                        XElement root = XElement.Parse(matches.First().ToXml());
                        XElement node = root.DescendantsAndSelf("{http://hl7.org/fhir}" + path.Element).FirstOrDefault();
                        if (node != null)
                        {
                            node.Name = node.Name.LocalName;
                            category[property.Name]["SnippetXML"] = node.ToString();
                        }
                        else
                        {
                            category[property.Name]["SnippetXML"] = "";
                        }
                         Dictionary<string, dynamic> jsonRoot =
                            JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(matches.First().ToJson(),
                                new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None });
                        if (jsonRoot != null && jsonRoot.Keys.Contains(path.Element))
                        {
                            category[property.Name]["SnippetJSON"] = "{" + $"\"{path.Element}\": \"{jsonRoot[path.Element]}\"" + "}";
                        }
                        else
                        {
                            category[property.Name]["SnippetJSON"] = "";
                        }
                    }
                    else
                    {
                        category[property.Name]["SnippetXML"] = matches.First().ToXml();
                        category[property.Name]["SnippetJSON"] = matches.First().ToJson();
                    }

                }
                else
                {
                    category[property.Name]["SnippetXML"] = "";
                    category[property.Name]["SnippetJSON"] = "";
                }

                // Add snippets for test
                FHIRPath pathTest = (FHIRPath)property.GetCustomAttributes().Last();
                var matchesTest = TestRecord.GetRecord().GetITypedElement().Select(pathTest.Path);
                if (matchesTest.Count() > 0)
                {
                    if (info.Type == Property.Types.TupleCOD)
                    {
                        // Make sure to grab all of the Conditions for COD
                        string xml = "";
                        string json = "";
                        foreach(var match in matchesTest)
                        {
                            xml += match.ToXml();
                            json += match.ToJson() + ",";
                        }
                        category[property.Name]["SnippetXMLTest"] = xml;
                        category[property.Name]["SnippetJSONTest"] = "[" + json + "]";
                    }
                    else if (!String.IsNullOrWhiteSpace(pathTest.Element))
                    {
                        // Since there is an "Element" for this path, we need to be more
                        // specific about what is included in the snippets.
                        XElement root = XElement.Parse(matchesTest.First().ToXml());
                        XElement node = root.DescendantsAndSelf("{http://hl7.org/fhir}" + pathTest.Element).FirstOrDefault();
                        if (node != null)
                        {
                            node.Name = node.Name.LocalName;
                            category[property.Name]["SnippetXMLTest"] = node.ToString();
                        }
                        else
                        {
                            category[property.Name]["SnippetXMLTest"] = "";
                        }
                         Dictionary<string, dynamic> jsonRoot =
                            JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(matchesTest.First().ToJson(),
                                new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None });
                        if (jsonRoot != null && jsonRoot.Keys.Contains(pathTest.Element))
                        {
                            category[property.Name]["SnippetJSONTest"] = "{" + $"\"{pathTest.Element}\": \"{jsonRoot[pathTest.Element]}\"" + "}";
                        }
                        else
                        {
                            category[property.Name]["SnippetJSONTest"] = "";
                        }
                    }
                    else
                    {
                        category[property.Name]["SnippetXMLTest"] = matchesTest.First().ToXml();
                        category[property.Name]["SnippetJSONTest"] = matchesTest.First().ToJson();
                    }

                }
                else
                {
                    category[property.Name]["SnippetXMLTest"] = "";
                    category[property.Name]["SnippetJSONTest"] = "";
                }

                // Compare values
                if (info.Type == Property.Types.Dictionary)
                {
                    // Special case for Dictionary; we want to be able to describe what each key means
                    Dictionary<string, string> valueReference = (Dictionary<string, string>)property.GetValue(ReferenceRecord.GetRecord());
                    Dictionary<string, string> valueTest = (Dictionary<string, string>)property.GetValue(TestRecord.GetRecord());
                    Dictionary<string, Dictionary<string, string>> moreInfo = new Dictionary<string, Dictionary<string, string>>();
                    bool match = true;
                    foreach (PropertyParam parameter in property.GetCustomAttributes().Reverse().Skip(1).Reverse().Skip(1))
                    {
                        moreInfo[parameter.Key] = new Dictionary<string, string>();
                        moreInfo[parameter.Key]["Description"] = parameter.Description;
                        if (valueReference != null && valueReference.ContainsKey(parameter.Key))
                        {
                            moreInfo[parameter.Key]["Value"] = valueReference[parameter.Key];
                        }
                        else
                        {
                            moreInfo[parameter.Key]["Value"] = null;
                        }
                        if (valueTest != null && valueTest.ContainsKey(parameter.Key))
                        {
                            moreInfo[parameter.Key]["FoundValue"] = valueTest[parameter.Key];
                        }
                        else
                        {
                            moreInfo[parameter.Key]["FoundValue"] = null;
                        }
                        if ((valueReference.ContainsKey(parameter.Key) && valueTest.ContainsKey(parameter.Key)) &&
                            (String.Equals((string)valueReference[parameter.Key], (string)valueTest[parameter.Key], StringComparison.OrdinalIgnoreCase))) {
                            // Equal
                            Correct += 1;
                            moreInfo[parameter.Key]["Match"] = "true";
                        } else if ((valueReference.ContainsKey(parameter.Key) && valueTest.ContainsKey(parameter.Key)) &&
                                    String.IsNullOrWhiteSpace((string)valueReference[parameter.Key]) &&
                                    String.IsNullOrWhiteSpace((string)valueTest[parameter.Key])) {
                            // Equal
                            Correct += 1;
                            moreInfo[parameter.Key]["Match"] = "true";
                        } else if (!valueReference.ContainsKey(parameter.Key) && !valueTest.ContainsKey(parameter.Key)) {
                            // Both null, equal
                            Incorrect += 1;
                            moreInfo[parameter.Key]["Match"] = "false";
                            match = false;
                        } else {
                            // Not equal
                            Incorrect += 1;
                            moreInfo[parameter.Key]["Match"] = "false";
                            match = false;
                        }
                        Total += 1;
                    }
                    category[property.Name]["Match"] = match ? "true" : "false";
                    category[property.Name]["Value"] = moreInfo;
                }
                else
                {
                    category[property.Name]["Value"] = property.GetValue(ReferenceRecord.GetRecord());
                    category[property.Name]["FoundValue"] = property.GetValue(TestRecord.GetRecord());
                    Total += 1;

                    // Compare values
                    if (info.Type == Property.Types.String)
                    {
                        if (String.Equals((string)property.GetValue(ReferenceRecord.GetRecord()), (string)property.GetValue(TestRecord.GetRecord()), StringComparison.OrdinalIgnoreCase))
                        {
                            Correct += 1;
                            category[property.Name]["Match"] = "true";
                        }
                        else
                        {
                            Incorrect += 1;
                            category[property.Name]["Match"] = "false";
                        }
                    }
                    else if (info.Type == Property.Types.StringDateTime)
                    {
                        DateTime referenceDateTime;
                        DateTime testDateTime;
                        if (property.GetValue(ReferenceRecord.GetRecord()) == null && property.GetValue(TestRecord.GetRecord()) == null)
                        {
                            Correct += 1;
                            category[property.Name]["Match"] = "true";
                        }
                        else if (DateTime.TryParse((string)property.GetValue(ReferenceRecord.GetRecord()), out referenceDateTime))
                        {
                            if (DateTime.TryParse((string)property.GetValue(TestRecord.GetRecord()), out testDateTime))
                            {
                                if (DateTime.Compare(referenceDateTime, testDateTime) == 0)
                                {
                                    Correct += 1;
                                    category[property.Name]["Match"] = "true";
                                }
                                else
                                {
                                    Incorrect += 1;
                                    category[property.Name]["Match"] = "false";
                                }
                            }
                            else
                            {
                                Incorrect += 1;
                                category[property.Name]["Match"] = "false";
                            }
                        }
                        else
                        {
                            Incorrect += 1;
                            category[property.Name]["Match"] = "false";
                        }
                    }
                    else if (info.Type == Property.Types.StringArr)
                    {
                        string[] referenceArr = (string[])property.GetValue(ReferenceRecord.GetRecord());
                        string[] testArr = (string[])property.GetValue(TestRecord.GetRecord());
                        if (referenceArr != null)
                        {
                            if (testArr != null)
                            {
                                if (String.Equals(String.Join(",", referenceArr.ToList().OrderBy(s => s).ToArray()), String.Join(",", testArr.ToList().OrderBy(s => s).ToArray())))
                                {
                                    Correct += 1;
                                    category[property.Name]["Match"] = "true";
                                }
                                else
                                {
                                    Incorrect += 1;
                                    category[property.Name]["Match"] = "false";
                                }
                            }
                            else
                            {
                                Incorrect += 1;
                                category[property.Name]["Match"] = "false";
                            }
                        }
                        else if (testArr != null)
                        {
                            Incorrect += 1;
                            category[property.Name]["Match"] = "false";
                        }
                        else
                        {
                            Correct += 1;
                            category[property.Name]["Match"] = "true";
                        }
                    }
                    else if (info.Type == Property.Types.Bool)
                    {
                        if (bool.Equals(property.GetValue(ReferenceRecord.GetRecord()), property.GetValue(TestRecord.GetRecord())))
                        {
                            Correct += 1;
                            category[property.Name]["Match"] = "true";
                        }
                        else
                        {
                            Incorrect += 1;
                            category[property.Name]["Match"] = "false";
                        }
                    }
                    else if (info.Type == Property.Types.TupleArr)
                    {
                        Tuple<string, string>[] referenceArr = (Tuple<string, string>[])property.GetValue(ReferenceRecord.GetRecord());
                        Tuple<string, string>[] testArr = (Tuple<string, string>[])property.GetValue(TestRecord.GetRecord());
                        if (referenceArr != null)
                        {
                            if (testArr != null)
                            {
                                if (String.Equals(String.Join(",", referenceArr.ToList().OrderBy(s => s.Item1 + s.Item2)), String.Join(",", testArr.ToList().OrderBy(s => s.Item1 + s.Item2))))
                                {
                                    Correct += 1;
                                    category[property.Name]["Match"] = "true";
                                }
                                else
                                {
                                    Incorrect += 1;
                                    category[property.Name]["Match"] = "false";
                                }
                            }
                            else
                            {
                                Incorrect += 1;
                                category[property.Name]["Match"] = "false";
                            }
                        }
                        else if (testArr != null)
                        {
                            Incorrect += 1;
                            category[property.Name]["Match"] = "false";
                        }
                        else
                        {
                            Correct += 1;
                            category[property.Name]["Match"] = "true";
                        }
                    }
                    else if (info.Type == Property.Types.TupleCOD)
                    {
                        Tuple<string, string, Dictionary<string, string>>[] referenceArr = (Tuple<string, string, Dictionary<string, string>>[])property.GetValue(ReferenceRecord.GetRecord());
                        Tuple<string, string, Dictionary<string, string>>[] testArr = (Tuple<string, string, Dictionary<string, string>>[])property.GetValue(TestRecord.GetRecord());
                        if (referenceArr != null)
                        {
                            if (testArr != null)
                            {
                                if (String.Equals(String.Join(",", referenceArr.ToList().OrderBy(s => s.Item1 + s.Item2)), String.Join(",", testArr.ToList().OrderBy(s => s.Item1 + s.Item2))))
                                {
                                    Correct += 1;
                                    category[property.Name]["Match"] = "true";
                                }
                                else
                                {
                                    Incorrect += 1;
                                    category[property.Name]["Match"] = "false";
                                }
                            }
                            else
                            {
                                Incorrect += 1;
                                category[property.Name]["Match"] = "false";
                            }
                        }
                        else if (testArr != null)
                        {
                            Incorrect += 1;
                            category[property.Name]["Match"] = "false";
                        }
                        else
                        {
                            Correct += 1;
                            category[property.Name]["Match"] = "true";
                        }
                    }
                }
            }
            Results = JsonConvert.SerializeObject(description);
        }
    }
}
