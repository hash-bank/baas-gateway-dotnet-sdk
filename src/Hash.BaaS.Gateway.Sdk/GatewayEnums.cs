namespace Hash.BaaS.Gateway.Sdk;

public enum AccountCloseReason { ClosedByCardholder, ClosedByClient, ClosedByIssuer, ClosedBySystem }
public enum AccountStatus { Active, Blocked, Closing, Closed }
public enum ApiBlockType { BlockedByCardUser, BlockedByCardholder, BlockedByCardholderViaPhone, BlockedByClient, BlockedByIssuer, Counterfeit, Fraudulent, Lost, Stolen }
public enum CardStatus { Created, Personalized, Ordered, Dispatched, Active, Blocked, Closing, Closed, Expired, AwaitingRenewal }
public enum CardType { Virtual, ChipAndPin, ChipAndPinAnonymous }
public enum DocumentType { Id = 1, Passport = 2, ResidencePermit = 3 }
public enum Gender { Male = 1, Female = 2 }
public enum IdvDocumentSubtype { NationalIDFront, NationalIDBack, Passport, ResidencePermitFront, ResidencePermitBack }
public enum IdvDocumentType { IDVDocument, IDVReport, IDVSelfieImage }
public enum IdvResultSource { Jumio, Onfido, Sumsub, Veriff }
public enum IdvResultStatus { Approved, Consider, Declined }
public enum KycCheckRejectReason { BlurryImage, DifferentBirthdate, DocumentDamaged, ExpiredDocument, InformationOnDocumentPartlyCovered, NameMismatch, PersonUnder18YearsOfAge, Other }
public enum KycCheckStatus { Mock, Created, Initiated, Pending, Approved, Rejected, Deleted }
public enum KycStatus { NotConfirmed, Pending, PendingManual, Rejected, Verified }
public enum PersonExpectedTurnover { From0to5000, From5001to15000, From15001to50000, From50001to100000, From100001to500000, From500001to1000000, From1000001AndMore }
public enum PersonExpectedMonthlyIncome { NoIncome, From0to1000, From1001to5000, From5001to10000, From10001to50000, From50001to100000, From100001AndMore }
public enum EmploymentWorkType { AMCServices, SoftwareServices, PreciousMetalsStones, AntiqueWorksArt, RealEstateAgent, Other }
public enum EmploymentType { Employed, SelfEmployed, Student, Retired, UnEmployed, BusinessOwner, Entrepreneurial, NotEmployed }
public enum PersonSourceOfIncome { NoIncome, EntrepreneurialActivity, Salary, MoneyTransfers, Rent, Pension, Scholarship, Business, FamilySupport, Dividends, Other }
public enum PersonAccountOpeningReason { BusinessGeorgia = 1, BackupAccountBusiness, SettlementsGeorgia, LivingStudying, FamilyTies, PreferentialDigital, HighDepositReturns, EmploymentBusiness, RealEstate, BackupAccountPersonal }
public enum PersonExpectedTransactionType { LocalWireTransfers = 1, CardPaymentsOrCashWithdrawal, CurrencyConversion, UtilityPayments, Other, Salary, DailyBanking, Savings, CreditProducts, WorldwidePaymentsAndTransfers, Crypto }
public enum BusinessActivityType { Other, AntiqueItems, UnregulatedServiceProvider, NonentrepreneurialLegalEntity, NongovernmentalOrganization, NonresidentAssetManagementCompany, ProductionNuclearMaterials, GrantIssuing, VirtualAssetServiceProvider, ProductionTradeMilitaryEquipment, OrganizerGames, MetalPreciousStones, PetroleumProducts, SoftwareServiceProvider, ReligiousOrganization, NonresidentInvestmentFund, TrustServiceProvider, CharitableOrganization, SportsClub, RealEstateAgency, PharmaceuticalProducts, CFDTradingCompany, ChemicalProducts, RelatedPreciousMetalsStones, HoldingCompany }
public enum PersonStatus { Active, Deactivated, Rejected }
public enum RiskProfile { Low, Medium, High }
public enum TransactionStatus { Authorized, Posted, Reversed, Released, Declined }
