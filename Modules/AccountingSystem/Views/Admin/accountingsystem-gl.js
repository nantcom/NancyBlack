(function () {

    var module = angular.module('AccountingGlModule', ['ui.bootstrap', 'angular.filter', 'chart.js']);

    module.controller("AccountingTransactionEditor", function ($scope, $rootScope, $http) {

        $me = this;
        $me.$scope = $scope;

        $me.invertIncrease = 1;
        $me.invertDecrease = 1;

        $me.refreshAutoComplete = function () {

            $http.get("/admin/tables/accountingentry/__autocompletes").success(function (data) {

                $scope.autocomplete = data;

            });
        };

        $me.changeMode = function (object) {

            $me.invertIncrease = 1;
            $me.invertDecrease = 1;
            object.DecreaseAccount = null;
            object.IncreaseAccount = null;
            object.IncreaseAmount = 0;
            object.DecreaseAmount = 0;

            if (object.TransactionType == "newclient") {

                object.DecreaseAmount = 0;
                object.DecreaseAccount = null;

                object.IncreaseAccount = "Receivable";
                object.IncreaseAmount = 0;
            }

            if (object.TransactionType == "futureexpense") {

                object.DecreaseAccount = null;

                object.IncreaseAccount = "Payable";
                $me.invertIncrease = -1;
            }

            if (object.TransactionType == "income") {

                object.IncreaseAccount = "Cash";
                $me.invertDecrease = -1;
            }

            if (object.TransactionType == "expense") {

                $me.invertDecrease = -1;
            }

            if (object.TransactionType == "buy") {

                $me.invertDecrease = -1;
            }
        };

        $me.save = function (object) {

            object.IncreaseAmount *= $me.invertIncrease;
            object.DecreaseAmount *= $me.invertDecrease;

            if ($me.ExpenseProjected == "Yes" && object.TransactionType == "expense") {

                object.IncreaseAccount = "Payable";
                object.IncreaseAmount = object.DecreaseAmount * -1;
            }

            if ($me.IncomeProjected == "Yes" && object.TransactionType == "income") {

                object.DecreaseAccount = "Receivable";
                object.DecreaseAmount = object.IncreaseAmount * -1;
            }

            if ($me.BuyAsEquipment == "Yes" && object.TransactionType == "buy") {

                object.IncreaseAccount = "Asset";
                object.IncreaseAmount = object.DecreaseAmount * -1;
            }

            if ($me.BuyAsEquipment == "No" && object.TransactionType == "buy") {

                object.IncreaseAccount = "Inventory";
                object.IncreaseAmount = object.DecreaseAmount * -1;
            }

            $scope.data.save(object, function () {
                
                $me.refreshAutoComplete();
            });
            $("#AccountingEntryModal").modal("hide");
        };

        $me.refreshAutoComplete();
    });

    module.controller("AccountingDashboard", function ($scope, $rootScope, $http) {

        $me = this;
        $scope.AccountSummary = [];

        $http.get("/admin/tables/accountingentry/__accountsummary").success(function (data) {

            for (var i = 0; i < data.TotalIncrease.length; i++) {

                var account = {
                    Account: data.TotalIncrease[i].Account,
                    TotalIncrease: data.TotalIncrease[i].Amount,
                    LatestIncreaseDate: data.TotalIncrease[i].LatestDate,
                };

                $scope.AccountSummary.push(account);
                $scope.AccountSummary[data.TotalIncrease[i].Account] = account;
            }

            for (var i = 0; i < data.TotalDecrease.length; i++) {

                var account = $scope.AccountSummary[data.TotalDecrease[i].Account];

                if (account == null) {
                    account = {
                        Account: data.TotalDecrease[i].Account,
                    }
                    $scope.AccountSummary.push(account);
                }

                account.TotalDecrease = data.TotalDecrease[i].Amount;
                account.LatestDecreaseDate = data.TotalDecrease[i].LatestDate;
                account.Amount = account.TotalIncrease + account.TotalDecrease;
            }
        });

    });

})();