using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;
using WholesaleApi.Services;
using Microsoft.EntityFrameworkCore;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController(OrderService orderService, AppDbContext db) : ControllerBase
{
    // Mağaza — sipariş oluştur
    [HttpPost]
    [Authorize(Roles = "Store")]
    public async Task<OrderDto> Create([FromBody] CreateOrderDto dto)
    {
        var storeId = await GetStoreId();
        return await orderService.CreateAsync(storeId, dto);
    }

    // Mağaza — kendi siparişlerini gör
    [HttpGet("my")]
    [Authorize(Roles = "Store")]
    public async Task<List<OrderDto>> MyOrders()
    {
        var storeId = await GetStoreId();
        return await orderService.GetForStoreAsync(storeId);
    }

    // Toptancı — gelen siparişler
    [HttpGet("incoming")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<List<OrderDto>> Incoming()
    {
        var wholesalerId = await GetWholesalerId();
        return await orderService.GetForWholesalerAsync(wholesalerId);
    }

    // Toptancı — sipariş durumu güncelle
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Wholesaler")]
    public async Task<OrderDto> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        var wholesalerId = await GetWholesalerId();
        if (!Enum.TryParse<OrderStatus>(dto.Status, out var status))
            throw new InvalidOperationException("Geçersiz sipariş durumu");
        return await orderService.UpdateStatusAsync(id, wholesalerId, status);
    }

    // Admin — tüm siparişler
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<List<OrderDto>> GetAll()
        => await orderService.GetAllAsync();

    [HttpGet("{id:guid}")]
    public async Task<OrderDto> GetById(Guid id)
        => await orderService.GetByIdAsync(id);

    private async Task<Guid> GetStoreId()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var store = await db.Stores.FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new KeyNotFoundException("Mağaza profili bulunamadı");
        return store.Id;
    }

    private async Task<Guid> GetWholesalerId()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var wholesaler = await db.Wholesalers.FirstOrDefaultAsync(w => w.UserId == userId)
            ?? throw new KeyNotFoundException("Toptancı profili bulunamadı");
        return wholesaler.Id;
    }
}
