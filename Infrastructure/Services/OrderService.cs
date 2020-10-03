using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;

namespace Infrastructure.Services
{
  public class OrderService : IOrderService
  {
    private readonly IBasketRepository _basketRepo;
    private readonly IUnitOfWork _unitOfWork;
    public OrderService(IBasketRepository basketRepo, IUnitOfWork unitOfWork)
    {
      _unitOfWork = unitOfWork;
      _basketRepo = basketRepo;
    }

    public async Task<Order> CreateOrderAsync(string buyerEmail, int deliveryMethodId, string basketId, Address shippingAddress)
    {
      CustomerBasket basket = await _basketRepo.GetBasketAsync(basketId);
      List<OrderItem> items = new List<OrderItem>();
      foreach (BasketItem item in basket.Items)
      {
        Product productItem = await _unitOfWork.Repository<Product>().GetByIdAsync(item.Id);
        ProductItemOrdered itemOrdered = new ProductItemOrdered(productItem.Id, productItem.Name, productItem.PictureUrl);
        OrderItem orderItem = new OrderItem(itemOrdered, productItem.Price, item.Quantity);
        items.Add(orderItem);
      }

      DeliveryMethod deliveryMethod = await _unitOfWork.Repository<DeliveryMethod>().GetByIdAsync(deliveryMethodId);

      decimal subtotal = items.Sum(item => item.Price * item.Quantity);

      Order order = new Order(items, buyerEmail, shippingAddress, deliveryMethod, subtotal);

      _unitOfWork.Repository<Order>().Add(order);

      int results = await _unitOfWork.Complete();

      if (results <= 0)
        return null;

      await _basketRepo.DeleteBasketAsync(basketId);

      return order;
    }

    public async Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync()
    {
      return await _unitOfWork.Repository<DeliveryMethod>().ListAllAsync();
    }

    public async Task<Order> GetOrderByIdAsync(int id, string buyerEmail)
    {
      OrdersWithItemsAndOrderingSpecification spec =
        new OrdersWithItemsAndOrderingSpecification(id, buyerEmail);

      return await _unitOfWork.Repository<Order>().GetEntityWithSpec(spec);
    }

    public async Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail)
    {
      OrdersWithItemsAndOrderingSpecification spec =
        new OrdersWithItemsAndOrderingSpecification(buyerEmail);

      return await _unitOfWork.Repository<Order>().ListAsync(spec);
    }
  }
}