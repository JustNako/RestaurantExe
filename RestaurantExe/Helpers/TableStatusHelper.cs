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
            Free,              // Свободен
            Occupied,          // Занят (есть активный заказ)
            ReservedNow,       // Забронирован сейчас
            ReservedLater      // Забронирован позже
        }

        public static TableStatus GetTableStatus(AppDbContext context, Table table, DateTime? checkTime = null)
        {
            var now = checkTime ?? DateTime.Now;

            // Проверяем, есть ли активный заказ
            var hasActiveOrder = context.Orders
                .Any(o => o.TableId == table.Id 
                    && !o.IsClosed 
                    && o.Status != OrderStatus.Cancelled);

            if (hasActiveOrder)
            {
                return TableStatus.Occupied;
            }

            // Проверяем активные бронирования
            var activeReservation = context.Reservations
                .Where(r => r.TableId == table.Id 
                    && r.Status == ReservationStatus.Active
                    && r.ReservationTime <= now
                    && (r.EndTime ?? r.ReservationTime.AddHours(2)) >= now)
                .OrderBy(r => r.ReservationTime)
                .FirstOrDefault();

            if (activeReservation != null)
            {
                return TableStatus.ReservedNow;
            }

            // Проверяем будущие бронирования
            var futureReservation = context.Reservations
                .Where(r => r.TableId == table.Id 
                    && r.Status == ReservationStatus.Active
                    && r.ReservationTime > now)
                .OrderBy(r => r.ReservationTime)
                .FirstOrDefault();

            if (futureReservation != null)
            {
                return TableStatus.ReservedLater;
            }

            return TableStatus.Free;
        }

        public static bool IsTableAvailableForOrder(AppDbContext context, int tableId, DateTime? orderTime = null)
        {
            var now = orderTime ?? DateTime.Now;
            var table = context.Tables.Find(tableId);
            
            if (table == null)
                return false;

            var status = GetTableStatus(context, table, now);

            // Стол доступен только если он свободен или забронирован позже
            // (можно обслуживать клиентов до брони)
            return status == TableStatus.Free || status == TableStatus.ReservedLater;
        }

        public static Reservation? GetCurrentReservation(AppDbContext context, int tableId, DateTime? checkTime = null)
        {
            var now = checkTime ?? DateTime.Now;
            
            return context.Reservations
                .Where(r => r.TableId == tableId 
                    && r.Status == ReservationStatus.Active
                    && r.ReservationTime <= now
                    && (r.EndTime ?? r.ReservationTime.AddHours(2)) >= now)
                .OrderBy(r => r.ReservationTime)
                .FirstOrDefault();
        }

        public static Reservation? GetNextReservation(AppDbContext context, int tableId, DateTime? checkTime = null)
        {
            var now = checkTime ?? DateTime.Now;
            
            return context.Reservations
                .Where(r => r.TableId == tableId 
                    && r.Status == ReservationStatus.Active
                    && r.ReservationTime > now)
                .OrderBy(r => r.ReservationTime)
                .FirstOrDefault();
        }
    }
}

