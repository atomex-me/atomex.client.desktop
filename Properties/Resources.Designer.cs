﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Atomex.Client.Desktop.Properties {
    using System;
    
    
    /// <summary>
    ///   Класс ресурса со строгой типизацией для поиска локализованных строк и т.д.
    /// </summary>
    // Этот класс создан автоматически классом StronglyTypedResourceBuilder
    // с помощью такого средства, как ResGen или Visual Studio.
    // Чтобы добавить или удалить член, измените файл .ResX и снова запустите ResGen
    // с параметром /str или перестройте свой проект VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Возвращает кэшированный экземпляр ResourceManager, использованный этим классом.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Atomex.Client.Desktop.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Перезаписывает свойство CurrentUICulture текущего потока для всех
        ///   обращений к ресурсу с помощью этого класса ресурса со строгой типизацией.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на You have active swaps. Closing the application or sign out may result in loss of funds as a result of the failure of the refund or redeem operations. Are you sure you want to close the application?.
        /// </summary>
        internal static string ActiveSwapsWarning {
            get {
                return ResourceManager.GetString("ActiveSwapsWarning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на The commission does not depend on the conversion amount and is determined only by the current network fees. The larger the convertible amount, the smaller the commission to amount ratio. Atomex blocks the conversion if the commission exceeds 75% of the conversion amount.
        /// </summary>
        internal static string CvAmountToFeeRatioToolTip {
            get {
                return ResourceManager.GetString("CvAmountToFeeRatioToolTip", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Amount is greater than the available. Please use the Max button to get the maximum available value..
        /// </summary>
        internal static string CvBigAmount {
            get {
                return ResourceManager.GetString("CvBigAmount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Conversion error. Please contant technical support..
        /// </summary>
        internal static string CvConversionError {
            get {
                return ResourceManager.GetString("CvConversionError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Error.
        /// </summary>
        internal static string CvError {
            get {
                return ResourceManager.GetString("CvError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на &quot;From&quot; address is empty.
        /// </summary>
        internal static string CvFromAddressIsNullOrEmpty {
            get {
                return ResourceManager.GetString("CvFromAddressIsNullOrEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Insufficient {0} to cover token transfer fee.
        /// </summary>
        internal static string CvInsufficientChainFunds {
            get {
                return ResourceManager.GetString("CvInsufficientChainFunds", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Insufficient funds.
        /// </summary>
        internal static string CvInsufficientFunds {
            get {
                return ResourceManager.GetString("CvInsufficientFunds", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Warning: too low fees.
        /// </summary>
        internal static string CvLowFees {
            get {
                return ResourceManager.GetString("CvLowFees", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на The amount must be greater than or equal to the minimum allowed amount {0} {1}..
        /// </summary>
        internal static string CvMinimumAllowedQtyWarning {
            get {
                return ResourceManager.GetString("CvMinimumAllowedQtyWarning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Not enough liquidity to convert a specified amount. In case you require more liquidity make multiple trades..
        /// </summary>
        internal static string CvNoLiquidity {
            get {
                return ResourceManager.GetString("CvNoLiquidity", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на This symbol does not support direct conversion..
        /// </summary>
        internal static string CvNotSupportedSymbol {
            get {
                return ResourceManager.GetString("CvNotSupportedSymbol", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Standard.
        /// </summary>
        internal static string CvOrderTypeStandard {
            get {
                return ResourceManager.GetString("CvOrderTypeStandard", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Standard With Fixed Fee.
        /// </summary>
        internal static string CvOrderTypeStandardWithFixedFee {
            get {
                return ResourceManager.GetString("CvOrderTypeStandardWithFixedFee", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Atomex services unavailable. Please check your network connection or contact technical support..
        /// </summary>
        internal static string CvServicesUnavailable {
            get {
                return ResourceManager.GetString("CvServicesUnavailable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Sufficient network fee for this amount ({0} is {1})..
        /// </summary>
        internal static string CvSufficientNetworkFee {
            get {
                return ResourceManager.GetString("CvSufficientNetworkFee", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Too high network fee for this amount ({0} is {1})!.
        /// </summary>
        internal static string CvTooHighNetworkFee {
            get {
                return ResourceManager.GetString("CvTooHighNetworkFee", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Wallet locked. Please unlock the wallet to continue..
        /// </summary>
        internal static string CvWalletLocked {
            get {
                return ResourceManager.GetString("CvWalletLocked", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Warning.
        /// </summary>
        internal static string CvWarning {
            get {
                return ResourceManager.GetString("CvWarning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Amount to convert must be greater than zero..
        /// </summary>
        internal static string CvWrongAmount {
            get {
                return ResourceManager.GetString("CvWrongAmount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Amount must be greater than zero..
        /// </summary>
        internal static string CvZeroAmount {
            get {
                return ResourceManager.GetString("CvZeroAmount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Back.
        /// </summary>
        internal static string CwvBack {
            get {
                return ResourceManager.GetString("CwvBack", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Cancel.
        /// </summary>
        internal static string CwvCancel {
            get {
                return ResourceManager.GetString("CwvCancel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Generate and write down the mnemonic before continuing!.
        /// </summary>
        internal static string CwvCreateMnemonicWarning {
            get {
                return ResourceManager.GetString("CwvCreateMnemonicWarning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Wallet name must be not empty..
        /// </summary>
        internal static string CwvEmptyWalletName {
            get {
                return ResourceManager.GetString("CwvEmptyWalletName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Finish.
        /// </summary>
        internal static string CwvFinish {
            get {
                return ResourceManager.GetString("CwvFinish", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Invalid wallet name..
        /// </summary>
        internal static string CwvInvalidWalletName {
            get {
                return ResourceManager.GetString("CwvInvalidWalletName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Invalid mnemonic phrase..
        /// </summary>
        internal static string CwvMnemonicInvalidError {
            get {
                return ResourceManager.GetString("CwvMnemonicInvalidError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Word count should be 12,15,18,21 or 24..
        /// </summary>
        internal static string CwvMnemonicInvalidWordcountError {
            get {
                return ResourceManager.GetString("CwvMnemonicInvalidWordcountError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Word {0} is not in the wordlist for this language..
        /// </summary>
        internal static string CwvMnemonicInvalidWordError {
            get {
                return ResourceManager.GetString("CwvMnemonicInvalidWordError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Mnemonic phrase can not be empty..
        /// </summary>
        internal static string CwvMnemonicIsEmptyError {
            get {
                return ResourceManager.GetString("CwvMnemonicIsEmptyError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Next.
        /// </summary>
        internal static string CwvNext {
            get {
                return ResourceManager.GetString("CwvNext", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Password has insufficient complexity..
        /// </summary>
        internal static string CwvPasswordInsufficientComplexity {
            get {
                return ResourceManager.GetString("CwvPasswordInsufficientComplexity", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Passwords do not match..
        /// </summary>
        internal static string CwvPasswordsDoNotMatch {
            get {
                return ResourceManager.GetString("CwvPasswordsDoNotMatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Scan.
        /// </summary>
        internal static string CwvScan {
            get {
                return ResourceManager.GetString("CwvScan", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Scan All.
        /// </summary>
        internal static string CwvScanAll {
            get {
                return ResourceManager.GetString("CwvScanAll", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Wallet with the same name already exists..
        /// </summary>
        internal static string CwvWalletAlreadyExists {
            get {
                return ResourceManager.GetString("CwvWalletAlreadyExists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на No tokens.
        /// </summary>
        internal static string PwNoTokens {
            get {
                return ResourceManager.GetString("PwNoTokens", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Amount must be greater than zero..
        /// </summary>
        internal static string SvAmountLessThanZeroError {
            get {
                return ResourceManager.GetString("SvAmountLessThanZeroError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на The transfer amount {0} with estimated commission {1} exceeds the amount of available funds!.
        /// </summary>
        internal static string SvAvailableFundsDetailedError {
            get {
                return ResourceManager.GetString("SvAvailableFundsDetailedError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на The transfer amount (including commission) exceeds the amount of available funds!.
        /// </summary>
        internal static string SvAvailableFundsError {
            get {
                return ResourceManager.GetString("SvAvailableFundsError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Back.
        /// </summary>
        internal static string SvBack {
            get {
                return ResourceManager.GetString("SvBack", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Cancel.
        /// </summary>
        internal static string SvCancel {
            get {
                return ResourceManager.GetString("SvCancel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Commission must be a positive value..
        /// </summary>
        internal static string SvCommissionLessThanZeroError {
            get {
                return ResourceManager.GetString("SvCommissionLessThanZeroError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Address must be not empty..
        /// </summary>
        internal static string SvEmptyAddressError {
            get {
                return ResourceManager.GetString("SvEmptyAddressError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Error.
        /// </summary>
        internal static string SvError {
            get {
                return ResourceManager.GetString("SvError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Failed.
        /// </summary>
        internal static string SvFailed {
            get {
                return ResourceManager.GetString("SvFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Gas limit:.
        /// </summary>
        internal static string SvGasLimit {
            get {
                return ResourceManager.GetString("SvGasLimit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Insufficient funds for default fee..
        /// </summary>
        internal static string SvInsufficientFundsForDefaultFeeError {
            get {
                return ResourceManager.GetString("SvInsufficientFundsForDefaultFeeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Address is invalid..
        /// </summary>
        internal static string SvInvalidAddressError {
            get {
                return ResourceManager.GetString("SvInvalidAddressError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Mining fee:.
        /// </summary>
        internal static string SvMiningFee {
            get {
                return ResourceManager.GetString("SvMiningFee", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на No.
        /// </summary>
        internal static string SvNo {
            get {
                return ResourceManager.GetString("SvNo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Ok.
        /// </summary>
        internal static string SvOk {
            get {
                return ResourceManager.GetString("SvOk", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Swap successfully created.
        ///
        ///NOTE: Do not sign out or close the application until the swap is completed, otherwise it may result in a loss of funds..
        /// </summary>
        internal static string SvOrderMatched {
            get {
                return ResourceManager.GetString("SvOrderMatched", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Order rejected..
        /// </summary>
        internal static string SvOrderRejected {
            get {
                return ResourceManager.GetString("SvOrderRejected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Oops, the price has changed during the order sending. Please try again..
        /// </summary>
        internal static string SvPriceHasChanged {
            get {
                return ResourceManager.GetString("SvPriceHasChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Send.
        /// </summary>
        internal static string SvSend {
            get {
                return ResourceManager.GetString("SvSend", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Send Confirmation.
        /// </summary>
        internal static string SvSendConfirmation {
            get {
                return ResourceManager.GetString("SvSendConfirmation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Sending....
        /// </summary>
        internal static string SvSending {
            get {
                return ResourceManager.GetString("SvSending", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Success.
        /// </summary>
        internal static string SvSuccess {
            get {
                return ResourceManager.GetString("SvSuccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Atomex is not responding for a long time..
        /// </summary>
        internal static string SvTimeoutReached {
            get {
                return ResourceManager.GetString("SvTimeoutReached", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Yes.
        /// </summary>
        internal static string SvYes {
            get {
                return ResourceManager.GetString("SvYes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Warning.
        /// </summary>
        internal static string Warning {
            get {
                return ResourceManager.GetString("Warning", resourceCulture);
            }
        }
    }
}
