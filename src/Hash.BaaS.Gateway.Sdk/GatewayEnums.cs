namespace Hash.BaaS.Gateway.Sdk;

public enum AccountCloseReason { ClosedByCardholder, ClosedByClient, ClosedByIssuer, ClosedBySystem }
public enum AccountStatus { Active, Blocked, Closing, Closed }
public enum ApiBlockType { BlockedByCardUser, BlockedByCardholder, BlockedByCardholderViaPhone, BlockedByClient, BlockedByIssuer, Counterfeit, Fraudulent, Lost, Stolen }
public enum CardStatus { Created, Personalized, Ordered, Dispatched, Active, Blocked, Closing, Closed, Expired, AwaitingRenewal }
public enum CardType { Virtual, ChipAndPin, ChipAndPinAnonymous }
public enum Gender { Male = 1, Female = 2 }
public enum IdvDocumentSubtype { NationalIDFront, NationalIDBack, Passport, ResidencePermitFront, ResidencePermitBack }
public enum IdvDocumentType { IDVDocument, IDVReport, IDVSelfieImage }
public enum IdvResultSource { Jumio, Onfido, Sumsub, Veriff }
public enum IdvResultStatus { Approved, Consider, Declined }
public enum KycCheckRejectReason { BlurryImage, DifferentBirthdate, DocumentDamaged, ExpiredDocument, InformationOnDocumentPartlyCovered, NameMismatch, PersonUnder18YearsOfAge, Other }
public enum KycCheckStatus { Mock, Created, Initiated, Pending, Approved, Rejected, Deleted }
public enum KycStatus { NotConfirmed, Pending, PendingManual, Rejected, Verified }
public enum PersonStatus { Active, Deactivated, Rejected }
public enum RestrictionType { Incasso, Seizure }
public enum StatementFormat { Xlsx, Xml, Pdf }
public enum TransactionStatus { Authorized, Posted, Reversed, Released, Declined }

// NOTE: The onboarding fields that used to be enums here — document_type, expected_turnover,
// expected_monthly_income, source_of_income, expected_transaction_type, account_opening_reason,
// employment_type, employment_work_type, business_activity_type, risk_profile — are now plain
// string CODES sourced from the gateway's reference-data catalogs. Fetch the active codes from
// GET /v1/embedded/reference-data/{type} (or /bundle) and submit one of those `code` values.
// See HashBaasGatewayClient.GetReferenceData* and the ReferenceData* response models.
