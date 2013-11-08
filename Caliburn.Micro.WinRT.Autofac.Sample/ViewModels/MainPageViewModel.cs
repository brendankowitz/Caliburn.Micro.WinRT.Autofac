using System.Collections.ObjectModel;
using System.Windows.Input;
using Caliburn.Micro.WinRT.Autofac.Sample.Entities;
using Caliburn.Micro.WinRT.Autofac.Sample.Events;
using Caliburn.Micro.WinRT.Autofac.Sample.Infrastructure;

namespace Caliburn.Micro.WinRT.Autofac.Sample.ViewModels
{
    public class MainPageViewModel : Screen, IHandle<ClearListEvent>
    {
        private readonly IEventAggregator _messenger;
        private readonly INavigationService _navigationService;

        public MainPageViewModel(IEventAggregator messenger, RightMenuViewModel rightMenuViewModel, INavigationService navigationService)
        {
            _messenger = messenger;
            _navigationService = navigationService;
            RightMenu = rightMenuViewModel;
            People = new ObservableCollection<Person>();
            _messenger.Subscribe(this);
            ToViewTwo = new ActionCommand(x => navigationService.NavigateToViewModel<SecondViewModel>());
        }

        private string _loadMessage;
        public string LoadMessage
        {
            get { return _loadMessage; }
            set { _loadMessage = value; NotifyOfPropertyChange(() => LoadMessage); }
        }

        private ObservableCollection<Person> _people;
        public ObservableCollection<Person> People
        {
            get { return _people; }
            set
            {
                _people = value;
                NotifyOfPropertyChange(() => People);
            }
        }

        public RightMenuViewModel RightMenu { get; set; }

        public ICommand ToViewTwo { get; set; }

        public void LoadPeople()
        {
            People = GetPeople();
        }

        private ObservableCollection<Person> GetPeople()
        {
            var people = new ObservableCollection<Person>();
            for (int i = 0; i < 10; i++)
            {
                var person = new Person
                                 {
                                     Name = string.Format("Name-{0}", i),
                                     Age = 10 + i
                                 };

                people.Add(person);
            }

            return people;
        }

        public void Handle(ClearListEvent message)
        {
            People.Clear();
        }
    }
}
