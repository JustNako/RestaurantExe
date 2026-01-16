using System;
using System.Linq;
using Restaurant.Data;
using Restaurant.Entities;
using Microsoft.EntityFrameworkCore;

namespace Restaurant.Helpers
{
    public static class TableStatusHelper
    {
        public enum TableStatus
        {
            Free,       // Свободен
            Occupied,   // Занят (есть активный заказ)
            Reserved    // Забронирован (неважно когда)
        }

        public static TableStatus GetTableStatus(
            AppDbContext context,
            Table table,
            DateTime? checkTime = null)
        {
            var now = checkTime ?? DateTime.Now;

            // 1️⃣ Есть активный заказ → стол занят
            var hasActiveOrder = context.Orders
                .Any(o =>
                    o.TableId == table.Id &&
                    !o.IsClosed &&
                    o.Status != OrderStatus.Cancelled);

            if (hasActiveOrder)
                return TableStatus.Occupied;

            // 2️⃣ Есть ЛЮБОЕ активное бронирование (текущее или будущее)
            var hasAnyReservation = context.Reservations
                .Any(r =>
                    r.TableId == table.Id &&
                    r.Status == ReservationStatus.Active &&
                    (
                        // текущее бронирование
                        (r.ReservationTime <= now &&
                         (r.EndTime ?? r.ReservationTime.AddHours(2)) >= now)

                        // или будущее
                        || r.ReservationTime > now
                    ));

            if (hasAnyReservation)
                return TableStatus.Reserved;

            // 3️⃣ Иначе — свободен
            return TableStatus.Free;
        }

        /// <summary>
        /// Можно ли создать заказ за столом
        /// </summary>
        public static bool IsTableAvailableForOrder(
            AppDbContext context,
            int tableId,
            DateTime? orderTime = null)
        {
            var now = orderTime ?? DateTime.Now;

            var table = context.Tables.Find(tableId);
            if (table == null)
                return false;

            // ❗ ВАЖНО:
            // Мы разрешаем заказ, если:
            // - стол свободен
            // - ИЛИ бронь есть, но она в будущем
            var hasActiveOrder = context.Orders
                .Any(o =>
                    o.TableId == tableId &&
                    !o.IsClosed &&
                    o.Status != OrderStatus.Cancelled);

            if (hasActiveOrder)
                return false;

            // Проверяем, есть ли ТЕКУЩАЯ бронь
            var hasCurrentReservation = context.Reservations
                .Any(r =>
                    r.TableId == tableId &&
                    r.Status == ReservationStatus.Active &&
                    r.ReservationTime <= now &&
                    (r.EndTime ?? r.ReservationTime.AddHours(2)) >= now);

            // Если бронь уже началась — нельзя
            if (hasCurrentReservation)
                return false;

            // Если брони нет или она в будущем — можно
            return true;
        }

        /// <summary>
        /// Текущее бронирование (если есть)
        /// </summary>
        public static Reservation? GetCurrentReservation(
            AppDbContext context,
            int tableId,
            DateTime? checkTime = null)
        {
            var now = checkTime ?? DateTime.Now;

            return context.Reservations
                .Where(r =>
                    r.TableId == tableId &&
                    r.Status == ReservationStatus.Active &&
                    r.ReservationTime <= now &&
                    (r.EndTime ?? r.ReservationTime.AddHours(2)) >= now)
                .OrderBy(r => r.ReservationTime)
                .FirstOrDefault();
        }

        /// <summary>
        /// Следующее бронирование (если есть)
        /// </summary>
        public static Reservation? GetNextReservation(
            AppDbContext context,
            int tableId,
            DateTime? checkTime = null)
        {
            var now = checkTime ?? DateTime.Now;

            return context.Reservations
                .Where(r =>
                    r.TableId == tableId &&
                    r.Status == ReservationStatus.Active &&
                    r.ReservationTime > now)
                .OrderBy(r => r.ReservationTime)
                .FirstOrDefault();
        }
    }
}

