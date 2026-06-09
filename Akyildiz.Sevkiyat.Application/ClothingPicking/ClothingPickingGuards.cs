using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    internal static class ClothingPickingGuards
    {
        /// <summary>İşlemi yalnızca işi alan toplayıcı veya yönetici yapabilir.</summary>
        public static void EnsureOwnerOrManager(Shipment s, ICurrentUserService user)
        {
            if (s.OperationType != OperationType.Clothing)
                throw new DomainException("Bu işlem yalnızca kıyafet sevkiyatları içindir.");

            bool isOwner = s.AssignedPickerId != null && s.AssignedPickerId == user.UserId;
            bool isManager = user.Role is UserRole.Admin or UserRole.Manager;
            if (!isOwner && !isManager)
                throw new ForbiddenException("Bu işlemi yalnızca işi alan toplayıcı veya yönetici yapabilir.");
        }

        /// <summary>
        /// Toplama satırı yazımı için ön koşul: mod seçilmiş olmalı; Cart modunda en az bir
        /// açık container bağlı olmalı. (Frontend'e güvenilmez — backend guard.)
        /// </summary>
        public static void EnsurePickingReady(Shipment s, bool hasOpenContainer)
        {
            if (s.PickingMode == null)
                throw new DomainException("Önce toplama modu seçilmelidir.");
            if (s.PickingMode == PickingMode.Cart && !hasOpenContainer)
                throw new DomainException("Araba (Cart) modunda toplamaya başlamadan en az bir araba bağlanmalıdır.");
        }
    }
}
