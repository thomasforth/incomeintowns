using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TownIncome
{
    class Program
    {
        static void Main(string[] args)
        {
            // load income by MSOA
            Console.WriteLine("Load Income by MSOA.");
            List<IncomeByMSOA> IncomesByMSOA = new List<IncomeByMSOA>();
            using (TextReader textReader = File.OpenText(@"C:\Users\Tom\Desktop\Towns and trains\1netannualincomeahc.csv"))
            {
                using (CsvReader csvReader = new CsvReader(textReader))
                {
                    IncomesByMSOA = csvReader.GetRecords<IncomeByMSOA>().ToList();
                }
            }

            // load OA classifications
            Console.WriteLine("Load OA classifications.");
            List<OAClassification> OAClassifications = new List<OAClassification>();
            using (TextReader textReader = File.OpenText(@"C:\Users\Tom\Desktop\Towns and trains\oa-classification-csv.csv"))
            {
                using (CsvReader csvReader = new CsvReader(textReader))
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
                if (IncomesByMSOA.Where(x => x.MSOAcode == OA.msoa_code).FirstOrDefault() != null)
                {
                    OA.NAIAHC = IncomesByMSOA.Where(x => x.MSOAcode == OA.msoa_code).FirstOrDefault().NAIAHC;
                    OA.NAIAHCtimesPopulation = OA.NAIAHC * OA.population;
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
                double totalNAIAC = OAsInThisPlace.Sum(x => x.NAIAHCtimesPopulation);

                PlaceAndIncome placeAndIncome = new PlaceAndIncome();
                placeAndIncome.Name = place;
                placeAndIncome.CityTownClassification = OAsInThisPlace.FirstOrDefault().citytownclassification;
                placeAndIncome.region_name = OAsInThisPlace.FirstOrDefault().region_name;
                placeAndIncome.la_name = OAsInThisPlace.FirstOrDefault().la_name;

                placeAndIncome.NAIAHC = totalNAIAC / totalPopulation;
                placeAndIncome.Population = totalPopulation;
                PlacesAndIncomes.Add(placeAndIncome);
            }

            // Print results
            Console.WriteLine("Write results.");
            using (TextWriter textWriter = File.CreateText(@"PlacesAndIncomes.csv"))
            {
                using (CsvWriter csvWriter = new CsvWriter(textWriter))
                {
                    csvWriter.WriteRecords(PlacesAndIncomes);
                }
            }

        }
    }

    public class IncomeByMSOA
    {
        public string MSOAcode { get; set; }
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
        public double population { get; set; }
        // Not in CSV, but added later
        public double NAIAHC { get; set; }
        public double NAIAHCtimesPopulation { get; set; }
    }

    public class PlaceAndIncome
    {
        public string Name { get; set; }
        public double NAIAHC { get; set; }
        public string CityTownClassification { get; set; }
        public double Population { get; set; }
        public string region_name { get; set; }
        public string la_name { get; set; }
    }
}
