using BattleShip.API.Validation;
using BattleShip.Models;
using BattleShip.Models.Dtos;

namespace BattleShip.Tests.Api;

public sealed class PlacementValidatorsTests
{
    private readonly PlaceShipRequestValidator placement = new();
    private readonly VersionedRequestValidator versioned = new();

    [Fact]
    public async Task Une_demande_de_placement_complete_est_valide()
    {
        var result = await placement.ValidateAsync(new PlaceShipRequest(3, 4, 5, Orientation.Vertical, 0));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null, 4, 5, Orientation.Vertical, 0, nameof(PlaceShipRequest.Column))]
    [InlineData(3, null, 5, Orientation.Vertical, 0, nameof(PlaceShipRequest.Row))]
    [InlineData(3, 4, null, Orientation.Vertical, 0, nameof(PlaceShipRequest.Length))]
    [InlineData(3, 4, 5, null, 0, nameof(PlaceShipRequest.Orientation))]
    [InlineData(3, 4, 5, Orientation.Vertical, null, nameof(PlaceShipRequest.ExpectedVersion))]
    [InlineData(3, 4, 0, Orientation.Vertical, 0, nameof(PlaceShipRequest.Length))]
    public async Task Une_demande_de_placement_incomplete_est_refusee_sur_le_bon_champ(
        int? column, int? row, int? length, Orientation? orientation, int? expectedVersion, string field)
    {
        var result = await placement.ValidateAsync(new PlaceShipRequest(column, row, length, orientation, expectedVersion));

        Assert.Equal(field, Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public async Task Une_case_hors_grille_n_est_pas_jugee_par_le_validateur_de_placement()
    {
        // Choix du binôme : les bornes dépendent de la partie et sont jugées par le moteur.
        var result = await placement.ValidateAsync(new PlaceShipRequest(99, -5, 5, Orientation.Horizontal, 0));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Une_commande_de_preparation_exige_sa_version_attendue()
    {
        Assert.True((await versioned.ValidateAsync(new VersionedRequest(0))).IsValid);
        Assert.Equal(
            nameof(VersionedRequest.ExpectedVersion),
            Assert.Single((await versioned.ValidateAsync(new VersionedRequest(null))).Errors).PropertyName);
        Assert.Equal(
            nameof(VersionedRequest.ExpectedVersion),
            Assert.Single((await versioned.ValidateAsync(new VersionedRequest(-1))).Errors).PropertyName);
    }
}
