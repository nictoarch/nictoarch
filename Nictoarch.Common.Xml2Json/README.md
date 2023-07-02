## Some proposed implementations for XML to JSON conversion:

### [Stefan Goessner](https://www.xml.com/pub/a/2006/05/31/converting-between-xml-and-json.html):
```xml
<e> 
	<a>text</a> 
	text 
	<a>text</a> 
</e>
```
```json
{
	"e": { 
		"#text": "text",
		"a": ["text", "text"] 
	}
}
```

### [IBM](https://www.ibm.com/docs/en/acvfc?topic=policies-xml-json-xml-json):
```xml
<a type="world">hello</a>
```

```json
{ 
	"a": { 
		"$" : "hello", 
		"@type" : "world" 
	} 
}
```

### [OxygenXML](https://www.oxygenxml.com/doc/versions/25.1/ug-editor/topics/convert-XML-to-JSON-x-tools.html):
```xml
<p>This <b>is</b> an <b>example</b>!</p>
```
```json
{
	"p": {
		"#text": "This ",
		"b": "is",
		"#text1": " an ",
		"b#1": "example",
		"#text2": "!"
	}
}
```

### [ReqBin](https://reqbin.com/xml-to-json)
```xml
<p>This <b att="val">is</b> an <b>example</b>!</p>
```
```json
{
	"p": {
        "_text": ["This ", " an ", "!"],
        "b": [
			{
				"_attributes": {
					"att": "val"
				},
				"_text": "is"
			}, 
			{
				"_text": "example"
			}
		]
    }
}
```

### The one we decided to implement:
```xml
<p>This <b att="val">is</b> an <b>example</b>!</p>
```
```json
{
    "_type": "p", 
    "_value": "This  an !",
    "@nested": [
        {
            "_type": "b",
            "att": "val",
            "_value": "is"
        },
        {
            "_type": "b",
            "_value": "example"
        },
    ]
}
```