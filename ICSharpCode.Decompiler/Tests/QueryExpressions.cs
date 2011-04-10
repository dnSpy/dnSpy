// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

public class QueryExpressions
{
	public class Customer
	{
		public int CustomerID;
		public IEnumerable<QueryExpressions.Order> Orders;
		public string Name;
		public string Country;
		public string City;
	}
	
	public class Order
	{
		public int OrderID;
		public DateTime OrderDate;
		public Customer Customer;
		public int CustomerID;
		public decimal Total;
		public IEnumerable<QueryExpressions.OrderDetail> Details;
	}
	
	public class OrderDetail
	{
		public decimal UnitPrice;
		public int Quantity;
	}
	
	public IEnumerable<QueryExpressions.Customer> customers;
	public IEnumerable<QueryExpressions.Order> orders;
	
	public object MultipleWhere()
	{
		return
			from c in this.customers
			where c.Orders.Count() > 10
			where c.Country == "DE"
			select c;
	}
	
	public object SelectManyFollowedBySelect()
	{
		return
			from c in this.customers
			from o in c.Orders
			select new { c.Name, o.OrderID, o.Total };
	}
	
	public object SelectManyFollowedByOrderBy()
	{
		return
			from c in this.customers
			from o in c.Orders
			orderby o.Total descending
			select new { c.Name, o.OrderID, o.Total };
	}
	
	public object MultipleSelectManyFollowedBySelect()
	{
		return
			from c in this.customers
			from o in c.Orders
			from d in o.Details
			select new { c.Name, o.OrderID, d.Quantity };
	}
	
	public object MultipleSelectManyFollowedByLet()
	{
		return
			from c in this.customers
			from o in c.Orders
			from d in o.Details
			let x = d.Quantity * d.UnitPrice
			select new { c.Name, o.OrderID, x };
	}
	
	public object FromLetWhereSelect()
	{
		return
			from o in this.orders
			let t = o.Details.Sum(d => d.UnitPrice * d.Quantity)
			where t >= 1000
			select new { o.OrderID, Total = t };
	}
	
	public object MultipleLet()
	{
		return
			from a in this.customers
			let b = a.Country
			let c = a.Name
			select b + c;
	}
	
	public object Join()
	{
		return
			from c in customers
			join o in orders on c.CustomerID equals o.CustomerID
			select new { c.Name, o.OrderDate, o.Total };
	}
	
	public object JoinInto()
	{
		return
			from c in customers
			join o in orders on c.CustomerID equals o.CustomerID into co
			let n = co.Count()
			where n >= 10
			select new { c.Name, OrderCount = n };
	}
	
	public object OrderBy()
	{
		return
			from o in orders
			orderby o.Customer.Name, o.Total descending
			select o;
	}
	
	public object GroupBy()
	{
		return
			from c in customers
			group c.Name by c.Country;
	}
	
	public object ExplicitType()
	{
		return
			from Customer c in customers
			where c.City == "London"
			select c;
	}
	
	public object QueryContinuation()
	{
		return
			from c in customers
			group c by c.Country into g
			select new { Country = g.Key, CustCount = g.Count() };
	}
}
