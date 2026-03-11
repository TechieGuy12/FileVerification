using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TE.FileVerification
{
    public class ArgumentsBinder : BinderBase<Arguments>
    {
        private readonly Option<string> _fileOption;
        private readonly Option<string> _checksumFileOption;
        private readonly Option<bool> _excludeSubDirOption;
        private readonly Option<HashAlgorithm> _algorithmOption;
        private readonly Option<string> _hashOption;
        private readonly Option<bool> _getHashOnlyOption;
        private readonly Option<int> _threadsOption;
        private readonly Option<string> _settingsFileOption;
        private readonly Option<bool> _removeFileOption;

        public ArgumentsBinder(
            Option<string> fileOption, 
            Option<string> checksumFileOption,
            Option<bool> excludeSubDirOption,
            Option<HashAlgorithm> algorithmOption,
            Option<string> hashOption,
            Option<bool> getHashOnlyOption,
            Option<int> threadsOption,
            Option<string> settingsFileOption,
            Option<bool> removeFileOption)
        {
            _fileOption = fileOption;
            _checksumFileOption = checksumFileOption;
            _excludeSubDirOption = excludeSubDirOption;
            _algorithmOption = algorithmOption;
            _hashOption = hashOption;
            _getHashOnlyOption = getHashOnlyOption;
            _threadsOption = threadsOption;
            _settingsFileOption = settingsFileOption;
            _removeFileOption = removeFileOption;
        }

        protected override Arguments GetBoundValue(BindingContext bindingContext) =>
            new Arguments
            {
                File = bindingContext.ParseResult.GetValueForOption(_fileOption),
                ChecksumFile = bindingContext.ParseResult.GetValueForOption(_checksumFileOption),
                ExcludeSubDir = bindingContext.ParseResult.GetValueForOption(_excludeSubDirOption),
                Algorithm = bindingContext.ParseResult.GetValueForOption(_algorithmOption),
                Hash = bindingContext.ParseResult.GetValueForOption(_hashOption),
                HashOnly = bindingContext.ParseResult.GetValueForOption(_getHashOnlyOption),
                Threads = bindingContext.ParseResult.GetValueForOption(_threadsOption),
                SettingsFile = bindingContext.ParseResult.GetValueForOption(_settingsFileOption),
                RemoveFile = bindingContext.ParseResult.GetValueForOption(_removeFileOption)
            };
    }
}
