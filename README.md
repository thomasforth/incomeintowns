# Income in towns
Calculating net annual income per head (after housing costs) for villages, town, and cities in England and Wales

## How to run
The code is written in .NET Core and will run on any computer (Linux, Mac, Windows) with the .NET runtime installed. I include the Visual Studio Solution which makes it easy to run on Windows.

The links in the source code are absolute and will need changing.

## Output
A single table is output, and included in this repo, with the following columns,
* Name
* NAIAHC (Net annual income after housing costs).
* CityTownClassification
* Population

## Sources
* [House of Commons Library version of Centre for Towns' City & Town Classification.]( https://researchbriefings.parliament.uk/ResearchBriefing/Summary/CBP-8322#fullreport)
* [Small area income estimates for middle layer super output areas, England and Wales, from the ONS]( https://www.ons.gov.uk/employmentandlabourmarket/peopleinwork/earningsandworkinghours/datasets/smallareaincomeestimatesformiddlelayersuperoutputareasenglandandwales). I use the *Net annual income (equivalised) after housing costs* CSV.

## Thanks
Without the work of [The Centre for Towns](https://www.centrefortowns.org/) and recent improvement to small area statistics by the ONS, this would have been possible.
