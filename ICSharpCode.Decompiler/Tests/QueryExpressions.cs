// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
		public QueryExpressions.Customer Customer;
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
		return from c in this.customers
			where c.Orders.Count<QueryExpressions.Order>() > 10
			where c.Country == "DE"
			select c;
	}
	
	public object SelectManyFollowedBySelect()
	{
		return from c in this.customers
			from o in c.Orders
			select new
			{
				c.Name,
				o.OrderID,
				o.Total
			};
	}
	
	public object SelectManyFollowedByOrderBy()
	{
		return from c in this.customers
			from o in c.Orders
			orderby o.Total descending
			select new
			{
				c.Name,
				o.OrderID,
				o.Total
			};
	}
	
	public object MultipleSelectManyFollowedBySelect()
	{
		return from c in this.customers
			from o in c.Orders
			from d in o.Details
			select new
			{
				c.Name,
				o.OrderID,
				d.Quantity
			};
	}
	
	public object MultipleSelectManyFollowedByLet()
	{
		return from c in this.customers
			from o in c.Orders
			from d in o.Details
			let x = d.Quantity * d.UnitPrice
			select new
			{
				c.Name,
				o.OrderID,
				x
			};
	}
	
	public object FromLetWhereSelect()
	{
		return from o in this.orders
			let t = o.Details.Sum((QueryExpressions.OrderDetail d) => d.UnitPrice * d.Quantity)
			where t >= 1000m
			select new
			{
				OrderID = o.OrderID,
				Total = t
			};
	}
	
	public object MultipleLet()
	{
		return from a in this.customers
			let b = a.Country
			let c = a.Name
			select b + c;
	}
	
	public object Join()
	{
		return from c in this.customers
			join o in this.orders on c.CustomerID equals o.CustomerID
			select new
			{
				c.Name,
				o.OrderDate,
				o.Total
			};
	}
	
	public object JoinInto()
	{
		return from c in this.customers
			join o in this.orders on c.CustomerID equals o.CustomerID into co
			let n = co.Count<QueryExpressions.Order>()
			where n >= 10
			select new
			{
				Name = c.Name,
				OrderCount = n
			};
	}
	
	public object OrderBy()
	{
		return from o in this.orders
			orderby o.Customer.Name, o.Total descending
			select o;
	}
	
	public object GroupBy()
	{
		return from c in this.customers
			group c.Name by c.Country;
	}
	
	public object ExplicitType()
	{
		return from QueryExpressions.Customer c in this.customers
			where c.City == "London"
			select c;
	}
	
	public object QueryContinuation()
	{
		return from c in this.customers
			group c by c.Country into g
			select new
			{
				Country = g.Key,
				CustCount = g.Count<QueryExpressions.Customer>()
			};
	}
}
