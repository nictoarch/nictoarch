## Some proposed implementations for XML to JSON conversion:

### [Stefan Goessner](https://www.xml.com/pub/a/2006/05/31/converting-between-xml-and-json.html):

<table>
<tr>
<td> XML </td> <td> JSON </td>
</tr>
<tr>
<td>
  
```xml
<e> 
  <a>text</a> 
  text 
  <a>text</a> 
</e>
```

</td>
<td>
  
```json
{
  "e": { 
    "#text": "text",
    "a": ["text", "text"] 
  }
}
```

</td>
</tr>
</table>

### [IBM](https://www.ibm.com/docs/en/acvfc?topic=policies-xml-json-xml-json):
<table>
<tr>
<td> XML </td> <td> JSON </td>
</tr>
<tr>
<td>
  
```xml
<a type="world">hello</a>
```

</td>
<td>
  
```json
{ 
  "a": { 
    "$" : "hello", 
    "@type" : "world" 
  } 
}
```

</td>
</tr>
</table>

### [OxygenXML](https://www.oxygenxml.com/doc/versions/25.1/ug-editor/topics/convert-XML-to-JSON-x-tools.html):
<table>
<tr>
<td> XML </td> <td> JSON </td>
</tr>
<tr>
<td>
  
```xml
<p>This <b>is</b> an <b>example</b>!</p>
```

</td>
<td>

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

</td>
</tr>
</table>

### [ReqBin](https://reqbin.com/xml-to-json)
<table>
<tr>
<td> XML </td> <td> JSON </td>
</tr>
<tr>
<td>
  
```xml
<p>This <b att="val">is</b> an <b>example</b>!</p>
```

</td>
<td>
  
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

</td>
</tr>
</table>

### The one we decided to implement:
<table>
<tr>
<td> XML </td> <td> JSON </td>
</tr>
<tr>
<td>

```xml
<p>This <b att="val">is</b> an <b>example</b>!</p>
```

</td>
<td>

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

</td>
</tr>
</table>
