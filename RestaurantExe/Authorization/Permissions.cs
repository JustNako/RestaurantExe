using System;

namespace Restaurant.Authorization
{
    public static class Permissions
    {
        public const string ClaimType = "permission";

        public static class Orders
        {
            public const string View = "Orders.View";
            public const string Create = "Orders.Create";
            public const string Edit = "Orders.Edit";
            public const string Delete = "Orders.Delete";
        }

        public static class Products
        {
            public const string View = "Products.View";
            public const string Create = "Products.Create";
            public const string Edit = "Products.Edit";
            public const string Delete = "Products.Delete";
        }

        public static class Categories
        {
            public const string View = "Categories.View";
            public const string Create = "Categories.Create";
            public const string Edit = "Categories.Edit";
            public const string Delete = "Categories.Delete";
        }

        public static class Reports
        {
            public const string View = "Reports.View";
        }

        public static class Roles
        {
            public const string Manage = "Roles.Manage";
        }

        public static class Users
        {
            public const string Manage = "Users.Manage";
        }

        public static class Shifts
        {
            public const string Open = "Shifts.Open";
            public const string Close = "Shifts.Close";
        }

        public static class Cash
        {
            public const string View = "Cash.View"; 
            public const string Edit = "Cash.Edit"; 
            public const string Close = "Cash.Close"; 
        }

        public static class Inventory
        {
            public const string View = "Inventory.View";
            public const string Manage = "Inventory.Manage"; // создание ингредиентов, рецептов, приход
        }

        public static readonly string[] All = new[]
        {
            Orders.View, Orders.Create, Orders.Edit, Orders.Delete,
            Products.View, Products.Create, Products.Edit, Products.Delete,
            Categories.View, Categories.Create, Categories.Edit, Categories.Delete,
            Reports.View,
            Roles.Manage,
            Users.Manage,
            Shifts.Open, Shifts.Close,
            Cash.View, Cash.Edit, Cash.Close,
            Inventory.View, Inventory.Manage
        };
    }
}






