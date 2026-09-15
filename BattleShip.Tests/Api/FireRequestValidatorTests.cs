using BattleShip.API.Validation;
using BattleShip.Models.Dtos;

namespace BattleShip.Tests.Api;

public sealed class FireRequestValidatorTests
{
    private readonly FireRequestValidator validator = new();

    [Fact]
    public async Task Une_requete_de_tir_complete_est_valide()
    {
        var result = await validator.ValidateAsync(new FireRequest(3, 4, 0));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null, 4, 0, nameof(FireRequest.Column))]
    [InlineData(3, null, 0, nameof(FireRequest.Row))]
    [InlineData(3, 4, null, nameof(FireRequest.ExpectedVersion))]
    public async Task Un_champ_absent_est_refuse_sur_ce_champ(int? column, int? row, int? expectedVersion, string missingField)
    {
        var result = await validator.ValidateAsync(new FireRequest(column, row, expectedVersion));

        var error = Assert.Single(result.Errors);
        Assert.Equal(missingField, error.PropertyName);
    }

    [Fact]
    public async Task Une_version_attendue_negative_est_refusee()
    {
        var result = await validator.ValidateAsync(new FireRequest(3, 4, -1));

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(FireRequest.ExpectedVersion), error.PropertyName);
    }

    [Fact]
    public async Task Une_case_hors_grille_n_est_pas_jugee_par_le_validateur()
    {
        // Choix du binôme : les bornes dépendent de la partie et sont jugées par le moteur.
        var result = await validator.ValidateAsync(new FireRequest(10, -1, 0));

        Assert.True(result.IsValid);
    }
}
