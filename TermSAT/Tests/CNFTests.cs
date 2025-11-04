using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TermSAT.Formulas;
using TermSAT.NandReduction;
using TermSAT.RuleDatabase;

namespace TermSAT.Tests;

[TestClass]
public class SatSolverTests
{
    [TestInitialize]
    public async Task InitializeTest()
    {


    }

    [TestCleanup]
    public void CleanupTest()
    {
    }


    [TestMethod]

    public void TestCNFExample1() 
    {

        RunCNFtest("cnf-example-1.txt");
	}

	public void TestEqAtreeBraun12Unsat() 
    {
		RunCNFtest("eq.atree.braun.12.unsat.cnf");
    }

    public void Testrpoc_xits_08_unsat() 
    {
		RunCNFtest("rpoc_xits_08_UNSAT.cnf");
	}

	public void TestSAT_Dat_k45() 
    {
		RunCNFtest("SAT_dat.k45.txt");
	}

	void RunCNFtest(String filename) 
    {
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        // Construct the full path to the file
        string filePath = Path.Combine(assemblyDirectory, filename);

        // Assert that the file exists
        Assert.IsTrue(File.Exists(filePath), $"File not found at: {filePath}");

        string fileContent = File.ReadAllText(filePath);

        // Perform assertions on the file content
        Assert.IsFalse(string.IsNullOrEmpty(fileContent), "File content is empty.");
    }


}
