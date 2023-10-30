- 📄 **`ncbitaxonmin.fsx`**: This file is used to auto generate `ncbitaxon.min`. The script is written in F# and requires [setup](https://learn.microsoft.com/en-us/dotnet/fsharp/get-started/get-started-vscode) to run.
- 📄 **`ncbitaxon.min`**: A auto generated file from the used species in research paper publications on: [https://www.ncbi.nlm.nih.gov](https://www.ncbi.nlm.nih.gov/pmc/advanced). It contains the 20.000 most used species directly findable in ncbitaxon. Any changes made to this file are lost, when it is auto generated again!
- 📄 **`ncbitaxon.min_plus.obo`**: This file contains any missing terms to ncbitaxonmin added by hand. As this file is not auto generated changes done here will not be lost.
- 📁 **`SpeciesResults.zip`**: Are the crawled results from [https://www.ncbi.nlm.nih.gov](https://www.ncbi.nlm.nih.gov/pmc/advanced) used for generating the latest version of `ncbitaxon.min`.

⚠️ If you update `ncbitaxon.min` or `ncbitaxon.min_plus.obo` please update the `data-version: 1.0.0` field in both files accordingly!