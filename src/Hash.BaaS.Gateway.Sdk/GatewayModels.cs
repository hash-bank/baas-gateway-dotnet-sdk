using System.Text.Json.Serialization;

namespace Hash.BaaS.Gateway.Sdk;

public sealed class StatusResponse
{
    public string? Status { get; set; }
    public string? Service { get; set; }
    public string? Version { get; set; }
}

public sealed class GetTermsResponse
{
    public IReadOnlyList<TermModel> Terms { get; init; } = [];
}

public sealed class CountryOnboardingRequirementsResponse
{
    public IReadOnlyList<CountryOnboardingRequirementModel> Countries { get; set; } = [];
}

public sealed class CountryOnboardingRequirementModel
{
    public string? CountryCode { get; set; }
    public string? Name { get; set; }
    public bool IsCountryServed { get; set; }
    public bool CanSubmitPerson { get; set; }
    public string? RiskGroupCode { get; set; }
    public bool IsHighRisk { get; set; }
    public bool RequiresResidencePermitForPassportIdentification { get; set; }
}

public sealed class TermModel
{
    /// <summary>Numeric id of a standardized term. Null for a custom term (use <see cref="TermCode"/>).</summary>
    public long? TermId { get; init; }

    /// <summary>Text code of a custom company+product term. Null for a standardized term.</summary>
    public string? TermCode { get; init; }
    public string? TermName { get; init; }
    public string? TermTitle { get; init; }
    public string? TermContent { get; init; }
    public string? Url { get; init; }
    public bool IsMandatory { get; init; }

    /// <summary>The value to send in applied_term_and_conditions: numeric id or custom code.</summary>
    public string? AppliedValue => TermId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? TermCode;
}

public sealed class CreatePersonRequest
{
    public required CreatePersonModel Person { get; set; }
}

public sealed class UpdatePersonRequest
{
    public required UpdatePersonModel Person { get; set; }
}

public sealed class CreatePersonResponse
{
    public PersonResponseModel Person { get; set; } = null!;
}

public sealed class GetPersonResponse
{
    public PersonResponseModel Person { get; set; } = null!;
}

public class CreatePersonModel
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? OriginalFirstName { get; set; }
    public string? OriginalLastName { get; set; }
    public string? MiddleName { get; set; }
    public string? FatherName { get; set; }
    public required string BirthDate { get; set; }
    public required Gender? Gender { get; set; }
    public required string BirthCity { get; set; }
    public required string BirthCountryCode { get; set; }
    public required string CitizenshipCountryCode { get; set; }
    public string? PersonalNumber { get; set; }
    public string? Title { get; set; }
    public string? PersonalNumberIssuerCountryCode { get; set; }
    public string? SecondaryCitizenshipCountryCode { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public required string DocumentNumber { get; set; }
    public required DocumentType? DocumentType { get; set; }
    public required string DocumentIssueDate { get; set; }
    public required string DocumentExpiryDate { get; set; }
    public required string DocumentIssuingAuthority { get; set; }
    public string? DocumentCountryCode { get; set; }
    public required string ResidenceCountryCode { get; set; }
    public required PersonAddressModel Address { get; set; }
    public required PersonAddressModel LegalAddress { get; set; }
    public bool? IsBeneficialOwner { get; set; }
    public bool? IsRepresentedBySomeoneElse { get; set; }
    public bool? IsPoliticallyExposedPerson { get; set; }
    public PersonPepDetailsModel? PoliticallyExposedPersonDetails { get; set; }
    public string? PoliticallyExposedPersonExplanation { get; set; }
    public bool? IsAdverseMediaInvolved { get; set; }
    public bool? IsSanctionsRelated { get; set; }
    public bool HasCrs { get; set; }
    public PersonCrsCountryModel[]? CrsCountries { get; set; }
    public bool HasGreenCard { get; set; }
    public PersonExpectedTurnover? ExpectedTurnover { get; set; }
    public PersonExpectedMonthlyIncome? ExpectedMonthlyIncome { get; set; }
    public PersonSourceOfIncome[]? SourcesOfIncome { get; set; }
    public required PersonExpectedTransactionType[] ExpectedTransactionTypes { get; set; }
    public PersonAccountOpeningReason[]? AccountOpeningReasons { get; set; }
    public RiskProfile? RiskProfile { get; set; }
    public required EmploymentType EmploymentType { get; set; }
    public EmploymentWorkType? EmploymentWorkType { get; set; }
    public PersonEmploymentDetailModel[]? EmploymentDetails { get; set; }
    public bool? IsSoleEntrepreneur { get; set; }
    public string? EntrepreneurTaxId { get; set; }
    public string? EntrepreneurBusinessActivity { get; set; }
    public required string[] AppliedTermAndConditions { get; set; }
    public string? ExternalId { get; set; }
    public string? PreferredLanguageCode { get; set; }
    public string? LoyaltyNumber { get; set; }
    public string? IpAddress { get; set; }
    public string? LastVerificationDate { get; set; }
}

public class UpdatePersonModel
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? OriginalFirstName { get; set; }
    public string? OriginalLastName { get; set; }
    public string? MiddleName { get; set; }
    public string? FatherName { get; set; }
    public string? BirthDate { get; set; }
    public Gender? Gender { get; set; }
    public string? BirthCity { get; set; }
    public string? BirthCountryCode { get; set; }
    public string? Nationality { get; set; }
    public string? PersonalNumber { get; set; }
    public string? PersonalNumberIssuer { get; set; }
    public string? SecondaryCitizenshipCountryCode { get; set; }
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? DocumentNumber { get; set; }
    public DocumentType? DocumentType { get; set; }
    public string? DocumentIssueDate { get; set; }
    public string? DocumentExpiryDate { get; set; }
    public string? DocumentIssuingAuthority { get; set; }
    public string? DocumentCountryCode { get; set; }
    public string? ResidenceCountryCode { get; set; }
    public PersonAddressModel? Address { get; set; }
    public PersonAddressModel? LegalAddress { get; set; }
    public bool? IsBeneficialOwner { get; set; }
    public bool? IsRepresentedBySomeoneElse { get; set; }
    public bool? IsPoliticallyExposedPerson { get; set; }
    public PersonPepDetailsModel? PepDetails { get; set; }
    public string? PoliticallyExposedPersonExplanation { get; set; }
    public bool? IsAdverseMediaInvolved { get; set; }
    public bool? IsSanctionsRelated { get; set; }
    public bool? HasCrs { get; set; }
    public bool? HasGreenCard { get; set; }
    public PersonCrsCountryModel[]? CrsCountries { get; set; }
    public PersonExpectedTurnover? ExpectedTurnover { get; set; }
    public string? ExpectedMonthlyIncomeCode { get; set; }
    public PersonSourceOfIncome[]? SourcesOfIncome { get; set; }
    public PersonExpectedTransactionType[]? ExpectedTransactionTypes { get; set; }
    public PersonAccountOpeningReason[]? AccountOpeningReasons { get; set; }
    public RiskProfile? RiskProfile { get; set; }
    public int? EmploymentTypeId { get; set; }
    public PersonEmploymentDetailModel[]? EmploymentDetails { get; set; }
    public bool? IsEntrepreneur { get; set; }
    public string? EntrepreneurTaxId { get; set; }
    public string? EntrepreneurBusinessActivity { get; set; }
    public string? ExternalId { get; set; }
    public string? PreferredLanguageCode { get; set; }
    public string? LoyaltyNumber { get; set; }
    public string? IpAddress { get; set; }
    public string? LastVerificationDate { get; set; }
}

public sealed class PersonAddressModel
{
    public required string Address1 { get; set; }
    public string? Address2 { get; set; }
    public required string City { get; set; }
    public string? PostalCode { get; set; }
    public required string CountryCode { get; set; }
}

public sealed class PersonCrsCountryModel
{
    public string? CountryCode { get; set; }
    public string? Tin { get; set; }
}

public sealed class PersonEmploymentDetailModel
{
    public required string CompanyName { get; set; }
    public required string Position { get; set; }
    public required BusinessActivityType BusinessActivityType { get; set; }
    public required string TaxId { get; set; }
    public string? Comment { get; set; }
}

public sealed class PersonPepDetailsModel
{
    public string? PepType { get; set; }
    public string? PepPosition { get; set; }
    public string? PepConnectionType { get; set; }
    public string? PepName { get; set; }
    public string? PepSurname { get; set; }
}

public class PersonResponseModel : UpdatePersonModel
{
    public string Id { get; set; } = null!;
    public long[]? AppliedTermIds { get; set; }
    public PersonStatus? Status { get; set; }
    public KycStatus? KycStatus { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}

public sealed class CreateKycCheckRequest
{
    public string PersonId { get; set; } = null!;
    public string ApplicantId { get; set; } = null!;
    public IdvResultDto? IdvResult { get; set; }
}

public sealed class CreateKycCheckResponse
{
    public string KycCheckId { get; set; } = null!;
    public KycCheckStatus? Status { get; set; }
}

public sealed class GetKycCheckResponse
{
    public KycCheckDto KycCheck { get; set; } = null!;
}

public sealed class IdvResultDto
{
    public IdvResultSource? Source { get; set; }
    public IdvResultStatus? Status { get; set; }
    public string? ReportLink { get; set; }
    public string? CreatedAt { get; set; }
}

public sealed class KycCheckDto
{
    public string Id { get; set; } = null!;
    public string PersonId { get; set; } = null!;
    public string? ApplicantId { get; set; }
    public KycCheckStatus? Status { get; set; }
    public KycCheckRejectReason? RejectReason { get; set; }
    public string? RejectReasonComment { get; set; }
    public IdvResultDto? IdvResult { get; set; }
    public string? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed class UploadKycDocumentRequest
{
    public required string KycCheckId { get; set; }
    public required IdvDocumentType Type { get; set; }
    public required Stream FileContent { get; set; }
    public required string FileName { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public IdvDocumentSubtype? Subtype { get; set; }
    public string? Number { get; set; }
    public string? Issuer { get; set; }
}

public sealed class UploadKycDocumentResponse
{
    public string KycDocumentId { get; set; } = null!;
}

public sealed class CreateAccountRequest
{
    public required string PersonId { get; set; }
    public string? CurrencyCode { get; set; }
    public string? ExternalId { get; set; }
    public string? Name { get; set; }
}

public sealed class CloseAccountPatchRequest
{
    public required AccountCloseReason CloseReason { get; set; }
}

public sealed class CreateAccountResponse
{
    public required AccountModel Account { get; set; }
}

public sealed class GetAccountResponse
{
    public AccountModel Account { get; set; } = null!;
}

public sealed class GetAccountCardsResponse
{
    public IReadOnlyList<CardResponseModel> Cards { get; set; } = [];
    public int TotalRecordsNumber { get; set; }
}

public sealed class AccountModel
{
    public string Id { get; set; } = null!;
    public string? PersonId { get; set; }
    public string? CurrencyCode { get; set; }
    public string? ExternalId { get; set; }
    public string? Name { get; set; }
    public AccountStatus? Status { get; set; }
    public double? Balance { get; set; }
    public double? BlockedAmount { get; set; }
    public double? AvailableAmount { get; set; }
    public int? CardsCount { get; set; }
    public string? ReferenceNumber { get; set; }
    public AccountCloseReason? CloseReason { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}

public sealed class CreateCardRequest
{
    public CreateCardModel Card { get; set; } = null!;
}

public sealed class CreateCardModel
{
    public required CardType Type { get; set; }
    public required string AccountId { get; set; }
    public string? EmbossingName { get; set; }
    public required string PersonalizationProductCode { get; set; }
    public string? ExternalId { get; set; }
    public string? EncryptedPin { get; set; }
    public CreateCardDeliveryAddress? DeliveryAddress { get; set; }
    public string? Comment { get; set; }
    public bool? IsDisposable { get; set; }
    public bool? DisableAutomaticRenewal { get; set; }
    public object? Limits { get; set; }
}

public sealed class CreateCardDeliveryAddress
{
    public required string Address1 { get; set; }
    public string? Address2 { get; set; }
    public required string City { get; set; }
    public required string PostalCode { get; set; }
    public required string CountryCode { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? DispatchMethod { get; set; }
}

public sealed class CreateCardResponse
{
    public CardResponseModel Card { get; set; } = null!;
}

public sealed class GetCardResponse
{
    public CardResponseModel Card { get; set; } = null!;
}

public sealed class ActivateCardResponse
{
    public CardResponseModel Card { get; set; } = null!;
}

public sealed class BlockCardRequest
{
    [JsonPropertyName("block_type")]
    public ApiBlockType BlockType { get; set; }
}

public sealed class BlockCardResponse
{
    public CardResponseModel Card { get; set; } = null!;
}

public sealed class UnblockCardResponse
{
    public CardResponseModel Card { get; set; } = null!;
}

public sealed class ResetPinCounterResponse
{
    public bool Success { get; set; } = true;
}

public sealed class DigitalCardViewResponse
{
    public DigitalCardWebViewLaunch WebView { get; set; } = null!;
}

public sealed class DigitalCardWebViewLaunch
{
    public string Method { get; set; } = "POST";
    public string Url { get; set; } = null!;
    public string ContentType { get; set; } = "application/x-www-form-urlencoded";
    public IDictionary<string, string> FormFields { get; set; } = new Dictionary<string, string>();
}

public sealed class GeneratePinKeyRequest
{
    public PinKeyRequestModel PinKeyRequest { get; set; } = new();
}

public sealed class PinKeyRequestModel
{
    public string? ChannelId { get; set; }
    public string? Language { get; set; }
    public string? DeviceId { get; set; }
}

public sealed class GeneratePinKeyResponse
{
    public PinKeyResponseModel PinKey { get; set; } = null!;
}

public sealed class PinKeyResponseModel
{
    public string RequestId { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public sealed class SetPinRequest
{
    public PinSetRequestModel PinSet { get; set; } = new();
}

public sealed class PinSetRequestModel
{
    public string RequestId { get; set; } = string.Empty;
    public string PinBlock { get; set; } = string.Empty;
    public string EncryptedSessionZpk { get; set; } = string.Empty;
}

public sealed class SetPinResponse
{
    public PinSetResponseModel PinSet { get; set; } = null!;
}

public sealed class PinSetResponseModel
{
    public string RequestId { get; set; } = string.Empty;
    public int ResultCode { get; set; }
}

public sealed class CardResponseModel
{
    public string Id { get; set; } = null!;
    public CardType Type { get; set; }
    public CardStatus Status { get; set; }
    public string AccountId { get; set; } = null!;
    public string? PersonId { get; set; }
    public string? ExternalId { get; set; }
    public string? MaskedCardNumber { get; set; }
    public string? EmbossingName { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? BlockType { get; set; }
    public object? Limits { get; set; }
    public CardDeliveryAddress? DeliveryAddress { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? PersonalizationProductCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsDisposable { get; set; }
    public bool RenewAutomatically { get; set; } = true;
    public string? PredecessorCardId { get; set; }
}

public sealed class CardDeliveryAddress
{
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
}

public sealed class CreateCorporateAccountRequest
{
    public CreateCorporateAccountModel Account { get; set; } = null!;
}

public sealed class CreateCorporateAccountModel
{
    public required IReadOnlyList<string> CurrencyCodes { get; set; }
    public string? Name { get; set; }
    public string? ExternalId { get; set; }
}

public sealed class CreateCorporateAccountResponse
{
    public IReadOnlyList<CorporateAccountModel> Accounts { get; set; } = [];
}

public sealed class ListCorporateAccountsResponse
{
    public IReadOnlyList<CorporateAccountModel> Accounts { get; set; } = [];
    public int TotalRecordsNumber { get; set; }
}

public sealed class GetCorporateAccountResponse
{
    public CorporateAccountModel Account { get; set; } = null!;
}

public sealed class GetCorporateAccountBalancesResponse
{
    public CorporateAccountBalancesModel Balances { get; set; } = null!;
}

public sealed class CorporateAccountBalancesModel
{
    public decimal Balance { get; set; }
    public decimal AvailableAmount { get; set; }
    public decimal BlockedAmount { get; set; }
    public decimal? CardBlocks { get; set; }
    public decimal? OtherBlocks { get; set; }
}

public sealed class GetCorporateAccountRequisitesResponse
{
    public CorporateAccountRequisitesModel Requisites { get; set; } = null!;
}

public sealed class CorporateAccountRequisitesModel
{
    public string AccountNumber { get; set; } = string.Empty;
    public string? BeneficiaryName { get; set; }
    public string? BeneficiaryNameEng { get; set; }
    public string? BankCode { get; set; }
    public string? ReferenceNumber { get; set; }
}

public sealed class GetCorporateAccountRestrictionsResponse
{
    public IReadOnlyList<CorporateAccountRestrictionModel> Restrictions { get; set; } = [];
}

public sealed class CorporateAccountRestrictionModel
{
    public RestrictionType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class GatewayFileResponse
{
    public required byte[] Content { get; init; }
    public string? ContentType { get; init; }
    public string? FileName { get; init; }
}

public sealed class RenameCorporateAccountRequest
{
    public RenameCorporateAccountModel Account { get; set; } = null!;
}

public sealed class RenameCorporateAccountModel
{
    public required string Name { get; set; }
}

public sealed class AddCorporateAccountCurrencyRequest
{
    public required string CurrencyCode { get; set; }
}

public sealed class CloseCorporateAccountRequest
{
    public AccountCloseReason? CloseReason { get; set; }
}

public sealed class CorporateAccountModel
{
    public Guid AccountId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? Name { get; set; }
    public AccountStatus Status { get; set; }
    public decimal Balance { get; set; }
    public decimal BlockedAmount { get; set; }
    public decimal AvailableAmount { get; set; }
    public int CardsCount { get; set; }
    public string? ReferenceNumber { get; set; }
    public AccountCloseReason? CloseReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class CreateCorporateCardRequest
{
    public CreateCorporateCardModel Card { get; set; } = new();
}

public sealed class CreateCorporateCardModel
{
    public string? AccountNumber { get; set; }
    public string? Currency { get; set; }
    public int CardDesignTypeId { get; set; }
    public bool IsVirtual { get; set; }
    public int? PackageId { get; set; }
    public int? TermId { get; set; }
    public string? Comment { get; set; }
    public string? NameOnCard { get; set; }
    public CreateCorporateCardDeliveryAddress? DeliveryAddress { get; set; }
    public AssigneePersonModel? Assignee { get; set; }
    public string? ExternalId { get; set; }
}

public sealed class CreateCorporateCardDeliveryAddress
{
    public string? CountryCode { get; set; }
    public long? CityId { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
}

public sealed class AssigneePersonModel
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? CitizenshipCountryCode { get; set; }
    public string? PersonalId { get; set; }
    public string? PassportNumber { get; set; }
    public string? Phone { get; set; }
}

public sealed class CreateCorporateCardResponse
{
    public CorporateCardModel Card { get; set; } = null!;
}

public sealed class ListCorporateCardDesignTypesResponse
{
    public IReadOnlyList<CorporateCardDesignTypeModel> DesignTypes { get; set; } = [];
}

public sealed class CorporateCardDesignTypeModel
{
    public int DesignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsVirtual { get; set; }
    public bool IsPhysical { get; set; }
    public bool HasInstant { get; set; }
    public string? CardBinTypeCode { get; set; }
}

public sealed class ListCorporateCardsResponse
{
    public IReadOnlyList<CorporateCardModel> Cards { get; set; } = [];
    public int TotalCount { get; set; }
    public int FromRecord { get; set; }
    public int RecordsCount { get; set; }
}

public sealed class GetCorporateCardResponse
{
    public CorporateCardModel Card { get; set; } = null!;
}

public sealed class FreezeCorporateCardResponse
{
    public CorporateCardModel Card { get; set; } = null!;
}

public sealed class UnfreezeCorporateCardResponse
{
    public CorporateCardModel Card { get; set; } = null!;
}

public sealed class ActivateCorporateCardResponse
{
    public CorporateCardModel Card { get; set; } = null!;
}

public sealed class CloseCorporateCardResponse
{
    public CorporateCardModel Card { get; set; } = null!;
}

public sealed class CorporateCardModel
{
    public Guid CardId { get; set; }
    public Guid AccountId { get; set; }
    public CardStatus Status { get; set; }
    public CardType Type { get; set; }
    public string? MaskedPan { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public string? CardholderName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed class ListCorporateTransactionsResponse
{
    public IReadOnlyList<CorporateTransactionModel> Transactions { get; set; } = [];
    public int TotalCount { get; set; }
    public int FromRecord { get; set; }
    public int RecordsCount { get; set; }
}

public sealed class GetCorporateTransactionResponse
{
    public CorporateTransactionModel Transaction { get; set; } = null!;
}

public sealed class CorporateTransactionModel
{
    public long TransactionId { get; set; }
    public Guid CardId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? MerchantName { get; set; }
    public string? MerchantCategory { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// ── Corporate FX: exchange + rates ──────────────────────────────────────────

/// <summary>Request body for POST /v1/corporate/transactions/exchanges — own-accounts market-rate FX.</summary>
public sealed class MakeCorporateExchangeRequest
{
    /// <summary>Source account number (debited leg). Required.</summary>
    public string? FromAccountNumber { get; set; }

    /// <summary>Target account number (credited leg). Required. May equal the source for an own-account conversion.</summary>
    public string? ToAccountNumber { get; set; }

    /// <summary>Amount to exchange, in the source currency. Must be at least 1.</summary>
    public decimal Amount { get; set; }

    /// <summary>Source currency (ISO 4217, three uppercase letters). Required.</summary>
    public string? Currency { get; set; }

    /// <summary>Target currency (ISO 4217). Required. Must differ from the source currency.</summary>
    public string? ToCurrency { get; set; }
}

/// <summary>Response for POST /v1/corporate/transactions/exchanges — identifier of the accepted exchange.</summary>
public sealed class MakeCorporateExchangeResponse
{
    public long Id { get; set; }
}

/// <summary>Response for GET /v1/corporate/rates when <c>amount</c> is omitted — the sell/buy rate board.</summary>
public sealed class CorporateRateBoardResponse
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public decimal SellRate { get; set; }
    public decimal BuyRate { get; set; }
}

/// <summary>Response for GET /v1/corporate/rates when <c>amount</c> is supplied — an indicative conversion quote.</summary>
public sealed class CorporateConversionQuoteResponse
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal ConvertedAmount { get; set; }
    public decimal Rate { get; set; }
}
