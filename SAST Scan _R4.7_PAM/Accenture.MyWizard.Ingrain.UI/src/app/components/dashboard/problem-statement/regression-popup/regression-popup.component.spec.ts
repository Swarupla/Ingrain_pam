import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RegressionPopupComponent } from './regression-popup.component';

describe('RegressionPopupComponent', () => {
  let component: RegressionPopupComponent;
  let fixture: ComponentFixture<RegressionPopupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RegressionPopupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RegressionPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
