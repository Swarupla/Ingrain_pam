import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TemplateNameModalComponent } from './template-name-modal.component';

describe('TemplateNameModalComponent', () => {
  let component: TemplateNameModalComponent;
  let fixture: ComponentFixture<TemplateNameModalComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TemplateNameModalComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TemplateNameModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
