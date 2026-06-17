<a href="https://www.flaticon.com/free-icon/translation_13481633" target="_blank">
   <img width="150" align="right" src="./resources/translator.png"></img>
</a>

# Translation Service

[![Python 3.13.7](https://img.shields.io/badge/.NET-10.0.301-purple.svg)](https://www.python.org/downloads/release/python-3137/)
[![Code style: CSharpier ](https://img.shields.io/badge/code%20style-CSharpier-32566c.svg)](https://github.com/belav/csharpier)

A lightweight C# translation microservice powered by [neural machine translation](https://en.wikipedia.org/wiki/Neural_machine_translation) (NMT) via [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate). The service has two endpoints, one for a single-key value translation (`POST /translate`) and one for batch translation (`POST /translate-bulk`).


<p align="center">
<img
src="./resources/translation-service.svg?raw=true"
alt="system overview"
width="100%"
class="center"
/>
<p align="center">
    <label><b>Fig. 1</b>: System overview</label>
    </p>
</p>

## Quick Start

```bash
cd translator
docker compose up --build
```

## Example usage

1. Translating exactly one key/value pair. (`POST /translate`)

```
{
  "target": "fr",
  "data": {
    "key-1": "Hello world"
  }
}
```

And the response would be:

```
{
  "key-1": "Bonjour le monde"
}
```

2. Translating multiple key/value pairs. (`POST /translate-bulk`)

```
{
  "target": "fr",
  "data": {
    "key-1": "Hello world",
    "key-2": "This is a simple description.",
    "key-3": "Simple text"
  }
}
```