import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { UserNotificationpopupComponent } from './user-notificationpopup.component';

describe('UserNotificationpopupComponent', () => {
  let component: UserNotificationpopupComponent;
  let fixture: ComponentFixture<UserNotificationpopupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ UserNotificationpopupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(UserNotificationpopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
