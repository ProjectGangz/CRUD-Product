using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;  // Correct namespace for SqlClient
using WebEcommerceClothing.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Configuration;
using Microsoft.Identity.Client;

public class ProductsController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    public ProductsController(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("MyConnectionString");
    }

    // Index Action
    // Index Action
    public IActionResult Index()
    {
        var products = new List<ProductModel>();

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Product";
            SqlCommand command = new SqlCommand(query, connection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                var product = new ProductModel
                {
                    ProductID = (int)reader["ProductID"],
                    CategoryID = (int)reader["CategoryID"],
                    ProductName = reader["ProductName"].ToString(),
                    ProductDescription = reader["ProductDescription"].ToString(),
                    Price = (decimal)reader["Price"],
                    ProductImage = reader["ProductImage"].ToString(),
                    Quantity = (int)reader["Quantity"],
                    Size = reader["Size"].ToString(),
                    Color = reader["Color"].ToString()
                };

                products.Add(product);
            }

            reader.Close();
        }

        // Load the Category navigation property
        foreach (var product in products)
        {
            string categoryQuery = $"SELECT CategoryID, CategoryName FROM Category WHERE CategoryID = {product.CategoryID}";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(categoryQuery, connection);
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    product.Category = new CategoryModel
                    {
                        CategoryID = (int)reader["CategoryID"],
                        CategoryName = reader["CategoryName"].ToString()
                    };
                }

                reader.Close();
            }
        }

        return View(products);
    }

    public IActionResult Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        ProductModel product = GetProductById((int)id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpGet]
    public IActionResult Create()
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "SELECT CategoryID, CategoryName FROM Category";
            SqlCommand command = new SqlCommand(query, connection);
            SqlDataReader reader = command.ExecuteReader();
            List<CategoryModel> categories = new List<CategoryModel>();
            while (reader.Read())
            {
                CategoryModel category = new CategoryModel
                {
                    CategoryID = Convert.ToInt32(reader["CategoryID"]),
                    CategoryName = reader["CategoryName"].ToString()
                };
                categories.Add(category);
            }
            ViewBag.Categories = categories;
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ProductModel product)
    {
        if (ModelState.IsValid)
        {
            string uniqueFileName = null;

            // If there is an image file
            if (product.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + product.ImageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file to the server
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    product.ImageFile.CopyTo(fileStream);
                }
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                INSERT INTO Product (CategoryID, ProductName, ProductDescription, Price, ProductImage, Quantity, Size, Color)
                VALUES (@CategoryID, @ProductName, @ProductDescription, @Price, @ProductImage, @Quantity, @Size, @Color)";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CategoryID", product.CategoryID);
                command.Parameters.AddWithValue("@ProductName", product.ProductName);
                command.Parameters.AddWithValue("@ProductDescription", product.ProductDescription);
                command.Parameters.AddWithValue("@Price", product.Price);
                command.Parameters.AddWithValue("@ProductImage", uniqueFileName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Quantity", product.Quantity);
                command.Parameters.AddWithValue("@Size", (object)product.Size ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Color", (object)product.Color ?? (object)DBNull.Value);
                command.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }
        return View(product);
    }


    [HttpGet]
    public IActionResult Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        ProductModel product = GetProductById((int)id);

        if (product == null)
        {
            return NotFound();
        }

        PopulateCategoriesDropDownList();
        return View(product);
    }
    private List<CategoryModel> GetCategories()
    {
        string? connectionString = _configuration.GetConnectionString("MyConnectionString");
        List<CategoryModel> categories = new List<CategoryModel>();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string query = "SELECT CategoryID, CategoryName FROM Category";
            SqlCommand command = new SqlCommand(query, connection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                CategoryModel category = new CategoryModel
                {
                    CategoryID = reader.GetInt32(reader.GetOrdinal("CategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName"))
                };
                categories.Add(category);
            }
        }

        return categories;
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, ProductModel product)
    {
        if (ModelState.IsValid)
        {
            string uniqueFileName = product.ProductImage;

            // Nếu có file ảnh mới được chọn
            if (product.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + product.ImageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Lưu file ảnh mới vào server
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    product.ImageFile.CopyTo(fileStream);
                }

                // Xóa file ảnh cũ
                if (!string.IsNullOrEmpty(product.ProductImage))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", product.ProductImage);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                product.ProductImage = uniqueFileName;
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
            UPDATE Product SET 
                CategoryID = @CategoryID, 
                ProductName = @ProductName, 
                ProductDescription = @ProductDescription, 
                Price = @Price, 
                ProductImage = @ProductImage, 
                Quantity = @Quantity, 
                Size = @Size, 
                Color = @Color 
            WHERE ProductID = @ProductID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CategoryID", product.CategoryID);
                command.Parameters.AddWithValue("@ProductName", product.ProductName);
                command.Parameters.AddWithValue("@ProductDescription", product.ProductDescription);
                command.Parameters.AddWithValue("@Price", product.Price);
                command.Parameters.AddWithValue("@ProductImage", (object)product.ProductImage ?? DBNull.Value);
                command.Parameters.AddWithValue("@Quantity", product.Quantity);
                command.Parameters.AddWithValue("@Size", (object)product.Size ?? DBNull.Value);
                command.Parameters.AddWithValue("@Color", (object)product.Color ?? DBNull.Value);
                command.Parameters.AddWithValue("@ProductID", id);
                command.ExecuteNonQuery();
            }

            return RedirectToAction(nameof(Index));
        }

        PopulateCategoriesDropDownList();
        return View(product);
    }



    private void PopulateCategoriesDropDownList()
    {
        ViewBag.Categories = GetCategories(); // Retrieve all categories and pass them to the view
    }

    public IActionResult Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        ProductModel product = GetProductById((int)id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        string? productImage = null;

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            // Lấy tên tệp ảnh của sản phẩm
            string getImageQuery = "SELECT ProductImage FROM Product WHERE ProductID = @ID";
            SqlCommand getImageCommand = new SqlCommand(getImageQuery, connection);
            getImageCommand.Parameters.AddWithValue("@ID", id);
            SqlDataReader reader = getImageCommand.ExecuteReader();

            if (reader.Read())
            {
                productImage = reader["ProductImage"].ToString();
            }
            reader.Close();

            // Xóa sản phẩm từ cơ sở dữ liệu
            string deleteQuery = "DELETE FROM Product WHERE ProductID = @ID";
            SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection);
            deleteCommand.Parameters.AddWithValue("@ID", id);
            deleteCommand.ExecuteNonQuery();
        }

        // Xóa tệp ảnh khỏi thư mục
        if (!string.IsNullOrEmpty(productImage))
        {
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", productImage);
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private ProductModel GetProductById(int id)
    {
        string? connectionString = _configuration.GetConnectionString("MyConnectionString");
        ProductModel? product = null;

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Product WHERE ProductID = @ID";
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ID", id);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    product = new ProductModel
                    {
                        ProductID = (int)reader["ProductID"],
                        CategoryID = (int)reader["CategoryID"],
                        ProductName = reader["ProductName"].ToString(),
                        ProductDescription = reader["ProductDescription"].ToString(),
                        Price = (decimal)reader["Price"],
                        ProductImage = reader["ProductImage"].ToString(),
                        Quantity = (int)reader["Quantity"],
                        Size = reader["Size"].ToString(),
                        Color = reader["Color"].ToString()
                    };
                }
            }
        }

        return product;
    }

    private void PopulateCategoriesDropDownList(object selectedCategory = null)
    {
        string? connectionString = _configuration.GetConnectionString("MyConnectionString");
        List<CategoryModel> categories = new List<CategoryModel>();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string query = "SELECT CategoryID, CategoryName FROM Category";
            SqlCommand command = new SqlCommand(query, connection);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    categories.Add(new CategoryModel
                    {
                        CategoryID = (int)reader["CategoryID"],
                        CategoryName = reader["CategoryName"].ToString()
                    });
                }
            }
        }

        ViewBag.Categories = new SelectList(categories, "CategoryID", "CategoryName", selectedCategory);
    }
}
