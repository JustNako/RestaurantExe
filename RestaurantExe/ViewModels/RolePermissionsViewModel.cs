using System.Collections.Generic;

namespace Restaurant.ViewModels
{
    public class RolePermissionsViewModel
    {
        public string RoleId { get; set; } = "";
        public string RoleName { get; set; } = "";
        public List<PermissionItem> Permissions { get; set; } = new();
    }

    public class PermissionItem
    {
        public string Name { get; set; } = "";
        public bool Selected { get; set; }
        public string Category { get; set; } = "";

        public string GetRussianName()
        {
            return Name switch
            {
                // Заказы
                "Orders.View" => "Просмотр заказов",
                "Orders.Create" => "Создание заказов",
                "Orders.Edit" => "Редактирование заказов",
                "Orders.Delete" => "Удаление заказов",
                
                // Продукты
                "Products.View" => "Просмотр продуктов",
                "Products.Create" => "Создание продуктов",
                "Products.Edit" => "Редактирование продуктов",
                "Products.Delete" => "Удаление продуктов",
                
                // Категории
                "Categories.View" => "Просмотр категорий",
                "Categories.Create" => "Создание категорий",
                "Categories.Edit" => "Редактирование категорий",
                "Categories.Delete" => "Удаление категорий",
                
                // Отчеты
                "Reports.View" => "Просмотр отчетов",
                
                // Роли
                "Roles.Manage" => "Управление ролями",
                
                // Пользователи
                "Users.Manage" => "Управление пользователями",
                
                // Смены
                "Shifts.Open" => "Открытие смены",
                "Shifts.Close" => "Закрытие смены",
                
                // Касса
                "Cash.View" => "Просмотр кассы",
                "Cash.Edit" => "Редактирование кассы",
                "Cash.Close" => "Закрытие кассы",
                
                _ => Name
            };
        }

        public string GetCategory()
        {
            return Name.Split('.')[0] switch
            {
                "Orders" => "Заказы",
                "Products" => "Продукты",
                "Categories" => "Категории",
                "Reports" => "Отчеты",
                "Roles" => "Роли",
                "Users" => "Пользователи",
                "Shifts" => "Смены",
                "Cash" => "Касса",
                _ => Name.Split('.')[0]
            };
        }
    }
}



