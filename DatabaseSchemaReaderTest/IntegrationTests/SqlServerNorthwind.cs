﻿using System.Diagnostics;
using System.Linq;
using DatabaseSchemaReader;
#if !NUNIT
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestContext = System.Object;
#endif

namespace DatabaseSchemaReaderTest.IntegrationTests
{
    /// <summary>
    /// Summary description for SqlServerNorthwind
    /// </summary>
    [TestClass]
    public class SqlServerNorthwind
    {

        [TestMethod]
        public void ReadNorthwindProducts()
        {
            var dbReader = TestHelper.GetNorthwindReader();
            var table = dbReader.Table("Products");
            Debug.WriteLine("Table " + table.Name);

            foreach (var column in table.Columns)
            {
                //because we loaded only a single table
                //relations aren't available here (to datatypes/foreign key tables)
                Debug.Write("\tColumn " + column.Name + "\t" + column.DbDataType);
                if (column.Length > 0) Debug.Write("(" + column.Length + ")");
                if (column.IsPrimaryKey) Debug.Write("\tPrimary key");
                if (column.IsForeignKey) Debug.Write("\tForeign key to " + column.ForeignKeyTableName);
                Debug.WriteLine("");
            }
            //Table Products
            //	Column ProductID	int	Primary key
            //	Column ProductName	nvarchar(40)
            //	Column SupplierID	int	Foreign key to Suppliers
            //	Column CategoryID	int	Foreign key to Categories
            //	Column QuantityPerUnit	nvarchar(20)
            //	Column UnitPrice	money
            //	Column UnitsInStock	smallint
            //	Column UnitsOnOrder	smallint
            //	Column ReorderLevel	smallint
            //	Column Discontinued	bit
        }

        [TestMethod]
        public void ReadNorthwindAllTables()
        {
            var dbReader = TestHelper.GetNorthwindReader();
            var tables = dbReader.AllTables();
            foreach (var table in tables)
            {
                Debug.WriteLine("Table " + table.Name);

                foreach (var column in table.Columns)
                {
                    //because we loaded only tables
                    //relations to datatypes aren't available here
                    //but foreign key tables are linked up
                    Debug.Write("\tColumn " + column.Name + "\t" + column.DbDataType);
                    if (column.Length > 0) Debug.Write("(" + column.Length + ")");
                    if (column.IsPrimaryKey) Debug.Write("\tPrimary key");
                    if (column.IsForeignKey) Debug.Write("\tForeign key to " + column.ForeignKeyTable.Name);
                    Debug.WriteLine("");
                }
            }
            //Table Products
            //	Column ProductID	int	Primary key
            //	Column ProductName	nvarchar(40)
            //	Column SupplierID	int	Foreign key to Suppliers
            //	Column CategoryID	int	Foreign key to Categories
            //	Column QuantityPerUnit	nvarchar(20)
            //	Column UnitPrice	money
            //	Column UnitsInStock	smallint
            //	Column UnitsOnOrder	smallint
            //	Column ReorderLevel	smallint
            //	Column Discontinued	bit
        }

        [TestMethod]
        public void ReadNorthwind()
        {
            var dbReader = TestHelper.GetNorthwindReader();
            var schema = dbReader.ReadAll();

            foreach (var table in schema.Tables)
            {
                Debug.WriteLine("Table " + table.Name);

                foreach (var column in table.Columns)
                {
                    Debug.Write("\tColumn " + column.Name + "\t" + column.DataType.TypeName);
                    if (column.DataType.IsString) Debug.Write("(" + column.Length + ")");
                    if (column.IsPrimaryKey) Debug.Write("\tPrimary key");
                    if (column.IsForeignKey) Debug.Write("\tForeign key to " + column.ForeignKeyTable.Name);
                    Debug.WriteLine("");
                }
                //Table Products
                //	Column ProductID	int	Primary key
                //	Column ProductName	nvarchar(40)
                //	Column SupplierID	int	Foreign key to Suppliers
                //	Column CategoryID	int	Foreign key to Categories
                //	Column QuantityPerUnit	nvarchar(20)
                //	Column UnitPrice	money
                //	Column UnitsInStock	smallint
                //	Column UnitsOnOrder	smallint
                //	Column ReorderLevel	smallint
                //	Column Discontinued	bit
            }
        }

        [TestMethod]
        public void ReadNorthwindViews()
        {
            var dbReader = TestHelper.GetNorthwindReader();
            var schema = dbReader.ReadAll();
            foreach (var view in schema.Views)
            {
                var sql = view.Sql;
                Assert.IsNotNull(sql, "ProcedureSource should also fill in the view source");
            }
        }

        [TestMethod]
        public void ReadNorthwindProductsWithCodeGen()
        {
            var dbReader = TestHelper.GetNorthwindReader();
            dbReader.DataTypes(); //load the datatypes
            var table = dbReader.Table("Products");
            Debug.WriteLine("Table " + table.Name);

            foreach (var column in table.Columns)
            {
                //Cs properties (the column name could be made .Net friendly too)
                Debug.WriteLine("\tpublic " + column.DataType.NetCodeName(column) + " " + column.Name + " { get; set; }");
            }
            //	public int ProductID { get; set; }
            //	public string ProductName { get; set; }
            //	public int SupplierID { get; set; }
            //	public int CategoryID { get; set; }
            //	public string QuantityPerUnit { get; set; }
            //	public decimal UnitPrice { get; set; }
            //	public short UnitsInStock { get; set; }
            //	public short UnitsOnOrder { get; set; }
            //	public short ReorderLevel { get; set; }
            //	public bool Discontinued { get; set; }

            //get the sql
            var sqlWriter =
                new SqlWriter(table, DatabaseSchemaReader.DataSchema.SqlType.SqlServer);
            var sql = sqlWriter.SelectPageSql(); //paging sql
            sql = SqlWriter.SimpleFormat(sql); //remove line breaks

            Debug.WriteLine(sql);
            //SELECT [ProductID], [ProductName], ...etc... 
            //FROM 
            //(SELECT ROW_NUMBER() OVER( ORDER BY [ProductID]) AS 
            //rowNumber, [ProductID], [ProductName],  ...etc..
            //FROM [Products]) AS countedTable 
            //WHERE rowNumber >= (@pageSize * (@currentPage - 1)) 
            //AND rowNumber <= (@pageSize * @currentPage)
        }


        [TestMethod]
        public void ReadNorthwindWithFilters()
        {
            //arrange
            const string category = "Categories";
            const string alphaList = "Alphabetical list of products";
            const string custorderhist = "CustOrderHist";
            var dbReader = TestHelper.GetNorthwindReader();
            dbReader.Exclusions.TableFilter.FilterExclusions.Add(category);
            dbReader.Exclusions.ViewFilter.FilterExclusions.Add(alphaList);
            dbReader.Exclusions.StoredProcedureFilter.FilterExclusions.Add(custorderhist);

            //act
            var schema = dbReader.ReadAll();

            //assert
            var table = schema.FindTableByName(category);
            Assert.IsNull(table);
            var view = schema.Views.FirstOrDefault(v => v.Name == alphaList);
            Assert.IsNull(view);
            var sproc = schema.StoredProcedures.FirstOrDefault(sp => sp.Name == custorderhist);
            Assert.IsNull(sproc);
        }

    }
}
