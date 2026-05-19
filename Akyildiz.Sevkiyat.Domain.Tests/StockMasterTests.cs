using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Domain.Tests;

public class StockMasterTests
{
    private static StockMaster Make(decimal onHand = 100, decimal reserved = 0)
    {
        var s = new StockMaster { StockCode = "TEST-001", StockName = "Test Stok" };
        if (onHand > 0)   s.Increase(onHand);
        if (reserved > 0) s.Reserve(reserved);
        return s;
    }

    // ── Reserve ──────────────────────────────────────────────────────────────

    [Fact]
    public void Reserve_IncreasesReservedQty()
    {
        var s = Make(onHand: 50);
        s.Reserve(20);
        Assert.Equal(20, s.ReservedQty);
    }

    [Fact]
    public void Reserve_DecreasesAvailableQty()
    {
        var s = Make(onHand: 50);
        s.Reserve(20);
        Assert.Equal(30, s.AvailableQty);
    }

    [Fact]
    public void Reserve_ExactAvailable_Succeeds()
    {
        var s = Make(onHand: 50);
        s.Reserve(50);
        Assert.Equal(0, s.AvailableQty);
    }

    [Fact]
    public void Reserve_ExceedsAvailable_ThrowsDomainException()
    {
        var s = Make(onHand: 50);
        Assert.Throws<DomainException>(() => s.Reserve(51));
    }

    [Fact]
    public void Reserve_ZeroQty_ThrowsDomainException()
    {
        var s = Make(onHand: 50);
        Assert.Throws<DomainException>(() => s.Reserve(0));
    }

    [Fact]
    public void Reserve_NegativeQty_ThrowsDomainException()
    {
        var s = Make(onHand: 50);
        Assert.Throws<DomainException>(() => s.Reserve(-5));
    }

    // ── ReleaseReservation ────────────────────────────────────────────────────

    [Fact]
    public void ReleaseReservation_DecreasesReservedQty()
    {
        var s = Make(onHand: 100, reserved: 30);
        s.ReleaseReservation(20);
        Assert.Equal(10, s.ReservedQty);
    }

    [Fact]
    public void ReleaseReservation_MoreThanReserved_ClampsToZero()
    {
        var s = Make(onHand: 100, reserved: 10);
        s.ReleaseReservation(50);
        Assert.Equal(0, s.ReservedQty);
    }

    [Fact]
    public void ReleaseReservation_Zero_IsNoOp()
    {
        var s = Make(onHand: 100, reserved: 20);
        s.ReleaseReservation(0);
        Assert.Equal(20, s.ReservedQty);
    }

    [Fact]
    public void ReleaseReservation_Negative_IsNoOp()
    {
        var s = Make(onHand: 100, reserved: 20);
        s.ReleaseReservation(-5);
        Assert.Equal(20, s.ReservedQty);
    }

    // ── Deduct ────────────────────────────────────────────────────────────────

    [Fact]
    public void Deduct_DecreasesOnHandAndReserved()
    {
        var s = Make(onHand: 100, reserved: 40);
        s.Deduct(30);
        Assert.Equal(70, s.OnHandQty);
        Assert.Equal(10, s.ReservedQty);
    }

    [Fact]
    public void Deduct_ExceedsOnHand_ThrowsDomainException()
    {
        var s = Make(onHand: 20);
        Assert.Throws<DomainException>(() => s.Deduct(21));
    }

    [Fact]
    public void Deduct_Zero_IsNoOp()
    {
        var s = Make(onHand: 50, reserved: 10);
        s.Deduct(0);
        Assert.Equal(50, s.OnHandQty);
        Assert.Equal(10, s.ReservedQty);
    }

    [Fact]
    public void Deduct_ReservedClampsToZero_WhenDeductExceedsReserved()
    {
        var s = Make(onHand: 100, reserved: 5);
        s.Deduct(50);
        Assert.Equal(50, s.OnHandQty);
        Assert.Equal(0, s.ReservedQty);
    }

    // ── Increase ─────────────────────────────────────────────────────────────

    [Fact]
    public void Increase_AddsToOnHandQty()
    {
        var s = Make(onHand: 30);
        s.Increase(20);
        Assert.Equal(50, s.OnHandQty);
    }

    [Fact]
    public void Increase_Zero_ThrowsDomainException()
    {
        var s = Make(onHand: 30);
        Assert.Throws<DomainException>(() => s.Increase(0));
    }

    [Fact]
    public void Increase_Negative_ThrowsDomainException()
    {
        var s = Make(onHand: 30);
        Assert.Throws<DomainException>(() => s.Increase(-1));
    }

    // ── AdjustOnHand ──────────────────────────────────────────────────────────

    [Fact]
    public void AdjustOnHand_PositiveDiff_IncreasesOnHand()
    {
        var s = Make(onHand: 50);
        s.AdjustOnHand(10);
        Assert.Equal(60, s.OnHandQty);
    }

    [Fact]
    public void AdjustOnHand_NegativeDiff_DecreasesOnHand()
    {
        var s = Make(onHand: 50);
        s.AdjustOnHand(-20);
        Assert.Equal(30, s.OnHandQty);
    }

    [Fact]
    public void AdjustOnHand_ExactZero_Succeeds()
    {
        var s = Make(onHand: 50);
        s.AdjustOnHand(-50);
        Assert.Equal(0, s.OnHandQty);
    }

    [Fact]
    public void AdjustOnHand_ResultNegative_ThrowsDomainException()
    {
        var s = Make(onHand: 50);
        Assert.Throws<DomainException>(() => s.AdjustOnHand(-51));
    }

    // ── OverrideOnHand ────────────────────────────────────────────────────────

    [Fact]
    public void OverrideOnHand_SetsOnHandToNewValue()
    {
        var s = Make(onHand: 50);
        s.OverrideOnHand(200);
        Assert.Equal(200, s.OnHandQty);
    }

    [Fact]
    public void OverrideOnHand_Zero_Succeeds()
    {
        var s = Make(onHand: 50);
        s.OverrideOnHand(0);
        Assert.Equal(0, s.OnHandQty);
    }

    [Fact]
    public void OverrideOnHand_Negative_ThrowsDomainException()
    {
        var s = Make(onHand: 50);
        Assert.Throws<DomainException>(() => s.OverrideOnHand(-1));
    }

    [Fact]
    public void OverrideOnHand_DoesNotChangeReservedQty()
    {
        var s = Make(onHand: 100, reserved: 30);
        s.OverrideOnHand(200);
        Assert.Equal(30, s.ReservedQty);
    }

    // ── AvailableQty computed property ────────────────────────────────────────

    [Fact]
    public void AvailableQty_IsOnHandMinusReserved()
    {
        var s = Make(onHand: 80, reserved: 25);
        Assert.Equal(55, s.AvailableQty);
    }
}
