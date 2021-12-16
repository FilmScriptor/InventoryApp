using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ManageProduct.Models;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.AspNetCore.Http;

namespace ManageProduct.Controllers;

public class HomeController : Controller
{
    private IConfiguration configuration;
    
    //The Azure Cosmos DB endpoint 
    private string EndpointUri;
    
    //The primary key for the Azure Cosmos account
    private string PrimaryKey;
    
    //the cosmos client instance
    private CosmosClient cosmosClient;

    //create database
    private Database database;

    //create container
    private Container container;

    //the name of the database and container
    private string databaseId = "ProductManagement";
    private string containerId = "Product";

    string storageConnectionString;

    CloudStorageAccount storageAccount;

    private string containerName = "products";

    private readonly ILogger<HomeController> _logger;

    //public HomeController(ILogger<HomeController> logger)
    //{
    //    _logger = logger;
    //    this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
    //}

    public HomeController(ILogger<HomeController> logger, IConfiguration config)
    {
        _logger = logger;
        configuration = config;
        EndpointUri = configuration.GetSection("CosmosDBEndpointUri").Value;
        PrimaryKey = configuration.GetSection("CosmosDBPrimaryKey").Value;
        storageConnectionString = configuration.GetSection("StorageConnectionString").Value;
        this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Index(Product product, IFormFile image)
    {
        CloudStorageAccount.TryParse(storageConnectionString, out storageAccount);
        CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
        CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
        cloudBlobContainer.CreateIfNotExists();

        CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(image.FileName);
        cloudBlockBlob.UploadFromStream(image.OpenReadStream());

        product.Id = Guid.NewGuid().ToString();
        product.ImagePath = cloudBlockBlob.Uri.ToString();
        this.database = this.cosmosClient.GetDatabase(databaseId);
        this.container = this.database.GetContainer(containerId);
        CreateProductAsync(product).Wait();
        return View();
    }

    private async Task CreateProductAsync(Product product)
    {
        // Create an item in the container representing the new product.
        ItemResponse<Product> productResponse = await this.container.CreateItemAsync<Product>(product);

        Console.WriteLine("Created product in database with id: {0} Operation consumed {1} RUs.\n", 
            productResponse.Resource.Id, productResponse.RequestCharge);
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public class Product
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public double Price { get; set; }
        public string ImagePath { get; set; }
    }

}
