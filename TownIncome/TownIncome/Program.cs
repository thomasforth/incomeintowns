using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TownIncome
{
    class Program
    {
        static void Main(string[] args)
        {
            // load place groupings
            Dictionary<string, string> PlaceGroupingsDictionary = new Dictionary<string, string>();
            using (TextReader textReader = File.OpenText(@"Assets/PlaceGroupings.csv"))
            {
                using (CsvReader csvReader = new CsvReader(textReader, CultureInfo.InvariantCulture))
                {
                    PlaceGroupingsDictionary = csvReader.GetRecords<TypeOfPlaceGrouping>().ToDictionary(x => x.TypeOfPlace, x => x.Grouping);
                }
            }

            // load commuting origins and destinations by MSOA
            Console.WriteLine("Load commuting origins and destinations by MSOA.");
            List<CommuterFlowIn> CommuterFlows = new List<CommuterFlowIn>();
            using (TextReader textReader = File.OpenText(@"Assets/wu03ew_msoa.csv"))
            {
                using (CsvReader csvReader = new CsvReader(textReader, CultureInfo.InvariantCulture))
                {
                    csvReader.Configuration.RegisterClassMap<CommuterFlowClassMap>();
                    csvReader.Configuration.HeaderValidated = null;
                    csvReader.Configuration.MissingFieldFound = null;
                    CommuterFlows = csvReader.GetRecords<CommuterFlowIn>().ToList();
                }
            }

            // load income by MSOA
            Console.WriteLine("Load Income by MSOA.");
            List<IncomeByMSOA> IncomesByMSOA = new List<IncomeByMSOA>();
            Dictionary<string, double> IncomesByMSOAByYearDictionary = new Dictionary<string, double>();
            using (TextReader textReader = File.OpenText(@"Assets/CombinedSmallIncomeData.csv"))
            {
                using (CsvReader csvReader = new CsvReader(textReader, CultureInfo.InvariantCulture))
                {
                    IncomesByMSOA = csvReader.GetRecords<IncomeByMSOA>().ToList();
                    IncomesByMSOAByYearDictionary = IncomesByMSOA.ToDictionary(x => x.MSOAcode + x.Year, x => x.NAIAHC);
                }
            }

            // load OA classifications
            Console.WriteLine("Load OA classifications.");
            List<OAClassification> OAClassifications = new List<OAClassification>();
            using (TextReader textReader = File.OpenText(@"Assets/oa-classification-csv.csv"))
            {
                using (CsvReader csvReader = new CsvReader(textReader, CultureInfo.InvariantCulture))
                {
                    csvReader.Configuration.HeaderValidated = null;
                    csvReader.Configuration.MissingFieldFound = null;
                    OAClassifications = csvReader.GetRecords<OAClassification>().ToList();
                }
            }

            // join the two datasets, add the NAIAHC column to the OA Classifications table
            Console.WriteLine("Join both datasets.");
            foreach (OAClassification OA in OAClassifications)
            {
                if (PlaceGroupingsDictionary.ContainsKey(OA.citytownclassification))
                {
                    OA.citytownsupergroup = PlaceGroupingsDictionary[OA.citytownclassification];
                }

                if (IncomesByMSOAByYearDictionary.ContainsKey(OA.msoa_code + "2018"))
                {
                    // Deflators (CPI) into 2018 pounds)
                    OA.NAIAHC_2012 = IncomesByMSOAByYearDictionary[OA.msoa_code + "2012"] * 1.104;
                    OA.NAIAHC_2014 = IncomesByMSOAByYearDictionary[OA.msoa_code + "2014"] * 1.079;
                    OA.NAIAHC_2016 = IncomesByMSOAByYearDictionary[OA.msoa_code + "2016"] * 1.064;
                    OA.NAIAHC_2018 = IncomesByMSOAByYearDictionary[OA.msoa_code + "2018"] * 1.023;

                    OA.NAIAHCtimesPopulation = OA.NAIAHC_2018 * OA.population;
                }
            }

            List<PlaceAndIncome> PlacesAndIncomes = new List<PlaceAndIncome>();

            // foreach unique place, calculate the NAIAHC
            Console.WriteLine("Calculate income for each unique place.");
            List<string> UniquePlace = OAClassifications.Select(x => x.bua_name).Distinct().ToList();
            
            foreach (string place in UniquePlace)
            {
                List<OAClassification> OAsInThisPlace = OAClassifications.Where(x => x.bua_name == place).ToList();
                double totalPopulation = OAsInThisPlace.Sum(x => x.population);

                PlaceAndIncome placeAndIncome = new PlaceAndIncome();
                placeAndIncome.Name = place;
                placeAndIncome.CityTownClassification = OAsInThisPlace.FirstOrDefault().citytownclassification;
                placeAndIncome.CityTownGrouping = OAsInThisPlace.FirstOrDefault().citytownsupergroup;
                placeAndIncome.region_name = OAsInThisPlace.FirstOrDefault().region_name;
                placeAndIncome.la_name = OAsInThisPlace.FirstOrDefault().la_name;

                placeAndIncome.NAIAHC_2012 = Math.Round(OAsInThisPlace.Average(x => x.NAIAHC_2012),0);
                placeAndIncome.NAIAHC_2014 = Math.Round(OAsInThisPlace.Average(x => x.NAIAHC_2014),0);
                placeAndIncome.NAIAHC_2016 = Math.Round(OAsInThisPlace.Average(x => x.NAIAHC_2016),0);
                placeAndIncome.NAIAHC_2018 = Math.Round(OAsInThisPlace.Average(x => x.NAIAHC_2018),0);
                placeAndIncome.Population = totalPopulation;
                PlacesAndIncomes.Add(placeAndIncome);
            }

            // Create an MSOA to town name dictionary
            Console.WriteLine("Create an MSOA to town name dictionary.");
            List<string> UniqueMSOACodes = OAClassifications.Select(x => x.msoa_code).Distinct().ToList();
            Dictionary<string, string> MSOAtoTown = new Dictionary<string, string>();
            int MSOAcount = 0;
            Parallel.ForEach(UniqueMSOACodes, (MSOACode) =>
            {
                OAClassification oaclassification = OAClassifications.Where(x => x.msoa_code == MSOACode).FirstOrDefault();
                if (oaclassification != null)
                {
                    MSOAtoTown.TryAdd(MSOACode, oaclassification.bua_name);
                }
                else
                {
                    MSOAtoTown.TryAdd(MSOACode, null);
                }
                MSOAcount++;
            });

            // foreach unique place, calculate the flows to each other unique place
            // first 
            /*
            Console.WriteLine("Classifying commuter flows to towns.");
            Parallel.ForEach(CommuterFlows, (commuterFlowIn) =>
            {
                string ResidenceTown = null;
                MSOAtoTown.TryGetValue(commuterFlowIn.ResidenceMSOA, out ResidenceTown);
                commuterFlowIn.ResidenceTown = ResidenceTown;

                string WorkplaceTown = null;
                MSOAtoTown.TryGetValue(commuterFlowIn.WorkplaceMSOA, out WorkplaceTown);
                commuterFlowIn.WorkplaceTown = WorkplaceTown;
            });

            Console.WriteLine("Calculating commuter flows between towns.");
            List<CommuterFlowOut> CommuterFlowsBetweenTowns = new List<CommuterFlowOut>();
            int placeCount = 0;
            foreach (string origin in UniquePlace)
            {
                List<CommuterFlowIn> FlowFromOrigin = CommuterFlows.Where(x => x.ResidenceTown == origin).ToList();

                //foreach (string destination in UniquePlace)
                //{
                    Parallel.ForEach(UniquePlace, (destination) =>
                    {
                    //string destination = "Manchester BUASD";
                    CommuterFlowOut commuterFlowOut = new CommuterFlowOut
                    {
                        TownOfResidence = origin,
                        TownOfWork = destination
                    };

                    // get the commuter flows from the origin to the destination and sum them
                    List<CommuterFlowIn> FlowToDestination = FlowFromOrigin.Where(x => x.WorkplaceTown == destination).ToList();
                    commuterFlowOut.AllModes = FlowToDestination.Sum(x => x.AllModes);


                    if (commuterFlowOut.AllModes > 0)
                    {
                        commuterFlowOut.Bus = FlowToDestination.Sum(x => x.Bus);
                        commuterFlowOut.Rails = FlowToDestination.Sum(x => x.UndergroundMetroLightRailTram + x.Train);
                        commuterFlowOut.Car = FlowToDestination.Sum(x => x.CarDriver + x.CarPassenger);
                        commuterFlowOut.CycleOrWalk = FlowToDestination.Sum(x => x.Walk + x.Cycle);
                        CommuterFlowsBetweenTowns.Add(commuterFlowOut);
                    }
                //}
                });
                placeCount++;
                Console.WriteLine(origin + " done. " + placeCount +" of " + UniquePlace.Count + ".");
            }
            */
            // Calculate distinct place types and mean income per OA in each one
            List<string> TypesOfPlace = OAClassifications.Select(x => x.citytownclassification).Distinct().ToList();
            List<PlaceType> IncomesByPlaceType = new List<PlaceType>();
            foreach (string typeofplace in TypesOfPlace)
            {
                PlaceType placeType = new PlaceType();
                placeType.Type = typeofplace;
                placeType.MeanIncomeAHC_2012 = OAClassifications.Where(x => x.citytownclassification == typeofplace).Average(x => x.NAIAHC_2012);
                placeType.MeanIncomeAHC_2014 = OAClassifications.Where(x => x.citytownclassification == typeofplace).Average(x => x.NAIAHC_2014);
                placeType.MeanIncomeAHC_2016 = OAClassifications.Where(x => x.citytownclassification == typeofplace).Average(x => x.NAIAHC_2016);
                placeType.MeanIncomeAHC_2018 = OAClassifications.Where(x => x.citytownclassification == typeofplace).Average(x => x.NAIAHC_2018);
                IncomesByPlaceType.Add(placeType);
            }
            Console.WriteLine("Write type of place and income results.");
            using (TextWriter textWriter = File.CreateText(@"TypeOfPlaceAndIncome.csv"))
            {
                using (CsvWriter csvWriter = new CsvWriter(textWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(IncomesByPlaceType);
                }
            }

            List<string> GroupedTypesOfPlace = OAClassifications.Select(x => x.citytownsupergroup).Distinct().ToList();
            List<PlaceType> IncomesByGroupedPlaceType = new List<PlaceType>();
            foreach (string typeofplace in GroupedTypesOfPlace)
            {
                PlaceType placeType = new PlaceType();
                placeType.Type = typeofplace;
                placeType.MeanIncomeAHC_2012 = OAClassifications.Where(x => x.citytownsupergroup == typeofplace).Average(x => x.NAIAHC_2012);
                placeType.MeanIncomeAHC_2014 = OAClassifications.Where(x => x.citytownsupergroup == typeofplace).Average(x => x.NAIAHC_2014);
                placeType.MeanIncomeAHC_2016 = OAClassifications.Where(x => x.citytownsupergroup == typeofplace).Average(x => x.NAIAHC_2016);
                placeType.MeanIncomeAHC_2018 = OAClassifications.Where(x => x.citytownsupergroup == typeofplace).Average(x => x.NAIAHC_2018);
                IncomesByGroupedPlaceType.Add(placeType);
            }
            Console.WriteLine("Write type of place and income results.");
            using (TextWriter textWriter = File.CreateText(@"GroupedTypeOfPlaceAndIncome.csv"))
            {
                using (CsvWriter csvWriter = new CsvWriter(textWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(IncomesByGroupedPlaceType);
                }
            }

            
            // Print results
            Console.WriteLine("Write places and income results.");
            using (TextWriter textWriter = File.CreateText(@"PlacesAndIncomes.csv"))
            {
                using (CsvWriter csvWriter = new CsvWriter(textWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(PlacesAndIncomes);
                }
            }
            
            /*
            Console.WriteLine("Write places and income results.");
            using (TextWriter textWriter = File.CreateText(@"CommuterFlowsBetweenTowns.csv"))
            {
                using (CsvWriter csvWriter = new CsvWriter(textWriter))
                {
                    csvWriter.WriteRecords(CommuterFlowsBetweenTowns);
                }
            }
            */
        }
    }

    public class TypeOfPlaceGrouping
    {
        public string TypeOfPlace { get; set; }
        public string Grouping { get; set; }
    }


    public class PlaceType
    {
        public string Type { get; set; }
        public double MeanIncomeAHC_2012 { get; set; }        
        public double MeanIncomeAHC_2014 { get; set; }
        public double MeanIncomeAHC_2016 { get; set; }
        public double MeanIncomeAHC_2018 { get; set; }
    }

    public class IncomeByMSOA
    {
        public string MSOAcode { get; set; }

        public int Year { get; set; }
        public double NAIAHC { get; set; }
    }

    public class OAClassification
    {
        public string outputarea_code { get; set; }
        public string lsoa_code { get; set; }
        public string msoa_code { get; set; }
        public string la_code { get; set; }
        public string la_name { get; set; }
        public string region_name { get; set; }
        public string bua_code { get; set; }
        public string bua_name { get; set; }
        public string constituency_code { get; set; }
        public string constituency_name { get; set; }
        public string citytownclassification { get; set; }
        public string citytownsupergroup { get; set; }
        public double population { get; set; }
        // Not in CSV, but added later
        public double NAIAHC_2012 { get; set; }
        public double NAIAHC_2014 { get; set; }
        public double NAIAHC_2016 { get; set; }
        public double NAIAHC_2018 { get; set; }
        public double NAIAHCtimesPopulation { get; set; }
    }

    public class PlaceAndIncome
    {
        public string Name { get; set; }
        public double NAIAHC_2012 { get; set; }
        public double NAIAHC_2014 { get; set; }
        public double NAIAHC_2016 { get; set; }
        public double NAIAHC_2018 { get; set; }
        public string CityTownClassification { get; set; }
        public string CityTownGrouping { get; set; }
        public double Population { get; set; }
        public string region_name { get; set; }
        public string la_name { get; set; }
    }

    public class CommuterFlowOut
    {
        public string TownOfResidence { get; set; }
        public string TownOfWork { get; set; }
        public int AllModes { get; set; }
        public int Car { get; set; }
        public int Bus { get; set; }
        public int Rails { get; set; }
        public int CycleOrWalk { get; set; }
    }

    public class CommuterFlowIn
    {
        public string ResidenceMSOA { get; set; }
        public string WorkplaceMSOA { get; set; }
        public string ResidenceTown { get; set; }
        public string WorkplaceTown { get; set; }        
        public int AllModes { get; set; }
        public int UndergroundMetroLightRailTram { get; set; }
        public int Train { get; set; }
        public int CarDriver { get; set; }
        public int CarPassenger { get; set; }
        public int Bus { get; set; }
        public int Taxi { get; set; }
        public int Motorbike { get; set; }
        public int Rails { get; set; }
        public int Cycle { get; set; }
        public int Walk { get; set; }
        public int Other { get; set; }
    }

    public sealed class CommuterFlowClassMap : ClassMap<CommuterFlowIn>
    {
        public CommuterFlowClassMap()
        {
            Map(m => m.ResidenceMSOA).Name("Area of residence");
            Map(m => m.WorkplaceMSOA).Name("Area of workplace");
            Map(m => m.AllModes).Name("All categories: Method of travel to work");
            Map(m => m.UndergroundMetroLightRailTram).Name("Underground, metro, light rail, tram");
            Map(m => m.Train).Name("Train");
            Map(m => m.Bus).Name("Bus, minibus or coach");
            Map(m => m.Motorbike).Name("Motorcycle, scooter or moped");
            Map(m => m.Taxi).Name("Taxi");
            Map(m => m.CarDriver).Name("Driving a car or van");
            Map(m => m.CarPassenger).Name("Passenger in a car or van");
            Map(m => m.Cycle).Name("Bicycle");
            Map(m => m.Walk).Name("On foot");
            Map(m => m.Other).Name("Other method of travel to work");


            //"Area of residence",
            //"Area of workplace","All categories: Method of travel to work","Work mainly at or from home","Underground, metro, light rail, tram","Train","Bus, minibus or coach","Taxi","Motorcycle, scooter or moped","Driving a car or van","Passenger in a car or van","Bicycle","On foot","Other method of travel to work"
        }
    }



}
