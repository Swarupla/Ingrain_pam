import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { InfoConfirmPopupComponent } from './info-confirm-popup.component';

describe('InfoConfirmPopupComponent', () => {
  let component: InfoConfirmPopupComponent;
  let fixture: ComponentFixture<InfoConfirmPopupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ InfoConfirmPopupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(InfoConfirmPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
