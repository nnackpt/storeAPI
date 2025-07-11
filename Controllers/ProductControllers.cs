using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreAPI.Data;
using StoreAPI.Models;

namespace StoreAPI.Controllers;

[Authorize] // กำหนดให้ต้องมีการ Login ก่อนเข้าถึง API ทั้งหมด
// [Authorize(Roles = UserRoles.Admin)] // กำหนดให้เฉพาะ Admin เท่านั้นที่สามารถเข้าถึง API นี้ได้
[ApiController] // กำหนดให้ Class นี้เป็น API Controller
[Route("api/[controller]")] // กำหนด Route ของ API Controller
public class ProductController : ControllerBase
{
    // สร้าง Object ของ ApplicationDbContext
    private readonly ApplicationDbContext _context;

    private readonly IWebHostEnvironment _env;

    // สร้าง Constructor รับค่า ApplicationDbContext
    public ProductController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [AllowAnonymous]
    // ทดสอบเขียนฟังก์ชันการเชื่อมต่อ database
    // GET: /api/Product/testconnectdb
    [HttpGet("testconnectdb")]
    public void TestConnection()
    {
        // ถ้าเชื่อมต่อได้จะแสดงข้อความ "Connected"
        if (_context.Database.CanConnect())
        {
            Response.WriteAsync("Connected");
        }
        // ถ้าเชื่อมต่อไม่ได้จะแสดงข้อความ "Not Connected"
        else
        {
            Response.WriteAsync("Not Connected");
        }
    }

    // ฟังก์ชันสำหรับการดึงข้อมูลสินค้าทั้งหมด
    // GET: /api/Product?page=1&limit=10&selectedCategory=1&searchQuery=demo
    [HttpGet]
    public ActionResult<product> GetProducts(
        [FromQuery] int page=1,
        [FromQuery] int limit=100,
        [FromQuery] int? selectedCategory=null,
        [FromQuery] string? searchQuery=null
    )
    {
        // คำนวณหารายการที่จะข้าม
        int skip = (page - 1) * limit;

        // LINQ สำหรับการดึงข้อมูลจากตาราง Products ทั้งหมด
        // var products = _context.products.ToList();

        // แบบอ่านที่มีเงื่อนไข
        // var products = _context.products.Where(p => p.unit_price > 45000).ToList();

        // แบบเชื่อมกับตารางอื่น products เชื่อมกับ categories
        var query = _context.products
        .Join(
            _context.categories,
            p => p.category_id,
            c => c.category_id,
            (p, c) => new
            {
                p.product_id,
                p.product_name,
                p.unit_price,
                p.unit_in_stock,
                p.product_picture,
                p.created_date,
                p.modified_date,
                p.category_id,
                c.category_name
            }
        );

        // ถ้ามีการเลือกหมวดหมู่สินค้า
        if(selectedCategory.HasValue)
        {
            query = query.Where(p => p.category_id == selectedCategory.Value);
        }

        // ถ้ามีการค้นหาข้อมูล
        if (!string.IsNullOrEmpty(searchQuery))
        {
            // query = query.Where(p => p.product_name.Contains(searchQuery));
            query = query.Where(p => EF.Functions.ILike(p.product_name!, $"%{searchQuery}%"));
        }

        // นับจำนวนรายการทั้งหมด
        var totalRecords = query.Count(); // Count after filtering

        var products = query
            .OrderByDescending(p => p.product_id)
            .Skip(skip)
            .Take(limit)
            .ToList();

        // ส่งข้อมูลกลับไปให้ผู้ใช้งาน
        return Ok(new {Total = totalRecords, Products = products});
    }

    // ฟังก์ชันสำหรับการดึงข้อมูลสินค้าตาม id
    // GET: /api/Product/{id}
    [HttpGet("{id}")]
    public ActionResult<product> GetProduct(int id)
    {
        // LINQ สำหรับการดึงข้อมูลจากตาราง Products ตาม id
        var product = _context.products.FirstOrDefault(p => p.product_id == id);

        // ถ้าไม่พบข้อมูลจะแสดงข้อความ Not Found
        if (product == null)
        {
            return NotFound();
        }

        // ส่งข้อมูลกลับไปให้ผู้ใช้งาน
        return Ok(product);
    }

    // ฟังก์ชันสำหรับการเพิ่มข้อมูลสินค้า
    // POST: /api/Product
    // ฟังก์ชันสำหรับการเพิ่มข้อมูลสินค้า
    // POST: /api/Product
    [HttpPost]
    public async Task<ActionResult<product>> CreateProduct([FromForm] product product, IFormFile? image)
    {
        // เพิ่มข้อมูลลงในตาราง Products
        _context.products.Add(product);

        // ตรวจสอบว่ามีการอัพโหลดไฟล์รูปภาพหรือไม่
        if(image != null){
            // กำหนดชื่อไฟล์รูปภาพใหม่
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

            // บันทึกไฟล์รูปภาพ
            // string uploadFolder = Path.Combine(_env.ContentRootPath, "uploads");
            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");

            // ตรวจสอบว่าโฟลเดอร์ uploads มีหรือไม่
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            using (var fileStream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // บันทึกชื่อไฟล์รูปภาพลงในฐานข้อมูล
            product.product_picture = fileName;
        } else {
            product.product_picture = "noimg.jpg";
        }

        _context.SaveChanges();

        // ส่งข้อมูลกลับไปให้ผู้ใช้
        return Ok(product);
    }

    // ฟังก์ชันสำหรับการแก้ไขข้อมูลสินค้า
    // PUT /api/Product/1
    [HttpPut("{id}")]
    public async Task<ActionResult<product>> UpdateProduct(
        int id, [FromForm] 
        product product, 
        IFormFile? image
    )
    {
        // ดึงข้อมูลสินค้าตาม id
        var existingProduct = _context.products.FirstOrDefault(p => p.product_id == id);

        // ถ้าไม่พบข้อมูลจะแสดงข้อความ Not Found
        if (existingProduct == null)
        {
            return NotFound();
        }

        // แก้ไขข้อมูลสินค้า
        existingProduct.product_name = product.product_name;
        existingProduct.unit_price = product.unit_price;
        existingProduct.unit_in_stock = product.unit_in_stock;
        existingProduct.category_id = product.category_id;
        existingProduct.modified_date = product.modified_date;

        // ตรวจสอบว่ามีการอัพโหลดไฟล์รูปภาพหรือไม่
        if(image != null){
            // กำหนดชื่อไฟล์รูปภาพใหม่
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

            // บันทึกไฟล์รูปภาพ
            // string uploadFolder = Path.Combine(_env.ContentRootPath, "uploads");
            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");

            // ตรวจสอบว่าโฟลเดอร์ uploads มีหรือไม่
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            using (var fileStream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // ลบไฟล์รูปภาพเดิม ถ้ามีการอัพโหลดรูปภาพใหม่ และรูปภาพเดิมไม่ใช่ noimg.jpg
            if(existingProduct.product_picture != "noimg.jpg"){
                System.IO.File.Delete(Path.Combine(uploadFolder, existingProduct.product_picture!));
            }

            // บันทึกชื่อไฟล์รูปภาพลงในฐานข้อมูล
            existingProduct.product_picture = fileName;
        }

        // บันทึกข้อมูล
        _context.SaveChanges();

        // ส่งข้อมูลกลับไปให้ผู้ใช้
        return Ok(existingProduct);
    }

    // ฟังก์ชันสำหรับการลบข้อมูลสินค้า
    // DELETE /api/Product/1
    [HttpDelete("{id}")]
    public ActionResult<product> DeleteProduct(int id)
    {
        // ดึงข้อมูลสินค้าตาม id
        var product = _context.products.FirstOrDefault(p => p.product_id == id);

        // ถ้าไม่พบข้อมูลจะแสดงข้อความ Not Found
        if (product == null)
        {
            return NotFound();
        }

        // ตรวจสอบว่ามีไฟล์รูปภาพหรือไม่
        if(product.product_picture != "noimg.jpg"){
            // string uploadFolder = Path.Combine(_env.ContentRootPath, "uploads");
            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");

            // ลบไฟล์รูปภาพ
            System.IO.File.Delete(Path.Combine(uploadFolder, product.product_picture!));
        }

        // ลบข้อมูล
        _context.products.Remove(product);
        _context.SaveChanges();

        // ส่งข้อมูลกลับไปให้ผู้ใช้
        return Ok(product);
    }

}