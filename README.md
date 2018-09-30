# Income in towns
Calculating net annual income per head (after housing costs) for villages, town, and cities in England and Wales.

## WARNING
The ONS do not collect enough data to accurately estimate income at MSOA level. And so they use a model to calculate their best guess. I take this model output, match MSOAs to the places defined by Centre for Towns and The House of Commons Library (whose methods differ slightly, but The House of Commons data is easier to find) and then calculate the average net annual income per head (after housing costs) for that place. This is an average of an average figure created via a model. IF YOU DON'T UNDERSTAND HOW THE STATISTIC WAS CREATED, BE CAREFUL ABOUT USING IT.

## How to run
The code is written in .NET Core and will run on any computer (Linux, Mac, Windows) with the .NET runtime installed. I include the Visual Studio Solution which makes it easy to run on Windows.

The links in the source code are absolute and will need changing.

## Output
A single table is output, and included in this repo as *PlacesAndIncomes.csv*, with the following columns,
* Name
* NAIAHC (Net annual income after housing costs).
* CityTownClassification
* Population

## Sources
* [House of Commons Library version of Centre for Towns' City & Town Classification.]( https://researchbriefings.parliament.uk/ResearchBriefing/Summary/CBP-8322#fullreport)
* [Small area income estimates for middle layer super output areas, England and Wales, from the ONS]( https://www.ons.gov.uk/employmentandlabourmarket/peopleinwork/earningsandworkinghours/datasets/smallareaincomeestimatesformiddlelayersuperoutputareasenglandandwales). I use the *Net annual income (equivalised) after housing costs* CSV.

## Thanks
Without the work of [The Centre for Towns](https://www.centrefortowns.org/) and recent improvement to small area statistics by the ONS, this would have been possible.
